using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OccupancyCollector.Services.Zigbee.Abstractions;
using OccupancyCollector.Services.Zigbee.Listeners;
using OccupancyCollector.Services.Zigbee.Models;
using OccupancyCollector.Services.Zigbee.StateHandlers.Abstractions;
using ZigBeeNet;
using ZigBeeNet.App.Basic;
using ZigBeeNet.App.Discovery;
using ZigBeeNet.App.IasClient;
using ZigBeeNet.DataStore.Json;
using ZigBeeNet.Hardware.TI.CC2531;
using ZigBeeNet.Tranport.SerialPort;
using ZigBeeNet.ZCL.Clusters.General;

namespace OccupancyCollector.Services.Zigbee;

public class ZigbeeManager : IZigbeeManager
{
    private readonly ILogger<ZigbeeManager> _logger;
    private readonly IConfiguration _configuration;

    private ZigBeeNetworkManager _networkManager;
    private ZigBeeNode _coordinatorNode;

    private readonly List<ISensorStateHandler> _stateHandlers = new List<ISensorStateHandler>();
    private readonly List<OccupancySensor> _sensors = new List<OccupancySensor>();

    public ZigbeeManager(ILogger<ZigbeeManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public ZigBeeDongleTiCc2531 PrepareDongle()
    {
        var port = new ZigBeeSerialPort(_configuration.GetValue<string>("port"));
        var dongle = new ZigBeeDongleTiCc2531(port);
        
        dongle.SetLedMode(1, false); // green led
        dongle.SetLedMode(2, false); // red led

        return dongle;
    }
    
    public ZigBeeStatus Initialize(ZigBeeDongleTiCc2531 dongle)
    {
        // Prepare Zigbee network manager
        _networkManager = new ZigBeeNetworkManager(dongle);
        _networkManager.SetNetworkDataStore(new JsonNetworkDataStore("devices"));
        
        // Setup device discovery
        var discoveryExt = new ZigBeeDiscoveryExtension();
        discoveryExt.SetUpdatePeriod(60);
        
        // Add extensions and cluster support
        _networkManager.AddExtension(discoveryExt);
        _networkManager.AddExtension(new ZigBeeBasicServerExtension());
        _networkManager.AddExtension(new ZigBeeIasCieExtension());
        _networkManager.AddSupportedClientCluster(ZclIlluminanceLevelSensingCluster.CLUSTER_ID);
        _networkManager.AddSupportedClientCluster(ZclOccupancySensingCluster.CLUSTER_ID);

        // Add listeners
        _networkManager.AddCommandListener(new CommandListener(command =>
        {
            _logger.LogInformation($"Received Command: {command}");
            
            if(command is ReportAttributesCommand)
            {
                var attribruteReport = (ReportAttributesCommand)command;
                var sourceAddress = attribruteReport.SourceAddress.Address;

                switch (attribruteReport.ClusterId)
                {
                    case 1024:
                        UpdateSensor(
                            _networkManager.Nodes.First(n => n.NetworkAddress == sourceAddress).IeeeAddress.ToString(),
                            int.Parse(attribruteReport.Reports[0].AttributeValue.ToString()), null);
                        break;
                    case 1030:
                        UpdateSensor(
                            _networkManager.Nodes.First(n => n.NetworkAddress == sourceAddress).IeeeAddress.ToString(),
                            null, (byte)attribruteReport.Reports[0].AttributeValue == 1);
                        break;
                }
            }
            
        }));
        _networkManager.AddNetworkNodeListener(new NetworkNodeListener(
            node =>
            {
                _logger.LogInformation($"Added new node: {node.IeeeAddress}");
                UpdateSensor(node.IeeeAddress.ToString(), 0, false);
            },
            node =>
            {
                _logger.LogInformation($"Removed node: {node.IeeeAddress}");
                RemoveSensor(node);
            },
            node => _logger.LogInformation($"Updated node: {node.IeeeAddress}")));

        _networkManager.Initialize();
        Thread.Sleep(5000);
        
        _coordinatorNode = _networkManager.GetNode(0);
        Task.Run(ClearOccupancyState);

        return _networkManager.Startup(false);
    }

    private void RemoveSensor(ZigBeeNode node)
    {
        lock (_sensors)
            _sensors.Remove(_sensors.First(s => s.Id == node.IeeeAddress.ToString()));
    }
    
    private void UpdateSensor(string nodeIeee, int? illuminance, bool? occupied)
    {
        lock (_sensors)
        {
            var sensor = _sensors.FirstOrDefault(s => s.Id == nodeIeee);
            if (sensor == null)
            {
                // Add sensor
                sensor = new OccupancySensor(nodeIeee, illuminance ?? 0, occupied ?? false);
                _sensors.Add(sensor);
            }
            else
            {
                // Update sensor
                sensor.LastUpdate = DateTime.Now;
                sensor.Illuminance = illuminance ?? sensor.Illuminance;
                sensor.Occupied = occupied ?? sensor.Occupied;

                foreach (var handler in _stateHandlers)
                    handler.HandleSensorState(sensor);
            }
        }
    }

    private async Task ClearOccupancyState()
    {
        while (true)
        {
            lock (_sensors)
            {
                foreach (var sensor in _sensors)
                {
                    if (sensor.LastUpdate < DateTime.Now.AddSeconds(-_configuration.GetValue<int>("occupancy-timeout")))
                    {
                        if (sensor.Occupied)
                        {
                            sensor.Occupied = false;
                            sensor.LastUpdate = DateTime.Now;
                            foreach (var handler in _stateHandlers)
                                handler.HandleSensorState(sensor);
                        }
                    }
                }
            }
            
            await Task.Delay(3000);
        }
    }
    
    public void PermitNetworkJoining(bool permit)
    {
        _coordinatorNode.PermitJoin(permit);
        _logger.LogInformation($"Network joining {(permit ? "enabled" : "disabled")}");
    }

    public void RegisterSensorStateHandler(ISensorStateHandler sensorStateHandler)
        => _stateHandlers.Add(sensorStateHandler);

    public IEnumerable<OccupancySensor> GetSensors()
    {
        lock (_sensors)
            return _sensors.ToList();
    }

}