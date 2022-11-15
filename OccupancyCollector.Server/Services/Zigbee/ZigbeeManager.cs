using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OccupancyCollector.Services.Zigbee.Listeners;
using ZigBeeNet;
using ZigBeeNet.App.Basic;
using ZigBeeNet.App.Discovery;
using ZigBeeNet.App.IasClient;
using ZigBeeNet.DataStore.Json;
using ZigBeeNet.Hardware.TI.CC2531;
using ZigBeeNet.Tranport.SerialPort;

namespace OccupancyCollector.Services.Zigbee;

public class ZigbeeManager
{
    private ILogger<ZigbeeManager> _logger;
    private IConfiguration _configuration;

    private ZigBeeNetworkManager _networkManager;
    private ZigBeeNode _coordinatorNode;

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
        _networkManager.AddCommandListener(new CommandListener(command => _logger.LogInformation($"Received Command: {command}")));
        _networkManager.AddNetworkNodeListener(new NetworkNodeListener(
            node => _logger.LogInformation($"Added new node: {node.IeeeAddress}"),
            node => _logger.LogInformation($"Removed node: {node.IeeeAddress}"),
            node => _logger.LogInformation($"Updated node: {node.IeeeAddress}")));

        _networkManager.Initialize();
        Thread.Sleep(5000);
        
        _coordinatorNode = _networkManager.GetNode(0);

        return _networkManager.Startup(false);
    }

    public void PermitNetworkJoining(bool permit)
    {
        _coordinatorNode.PermitJoin(permit);
        _logger.LogInformation($"Network joining {(permit ? "enabled" : "disabled")}");
    }
    
}