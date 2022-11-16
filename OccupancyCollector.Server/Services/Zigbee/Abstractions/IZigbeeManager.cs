using System.Collections.Generic;
using OccupancyCollector.Services.Zigbee.Models;
using OccupancyCollector.Services.Zigbee.StateHandlers.Abstractions;
using ZigBeeNet;
using ZigBeeNet.Hardware.TI.CC2531;

namespace OccupancyCollector.Services.Zigbee.Abstractions;

public interface IZigbeeManager
{
    ZigBeeDongleTiCc2531 PrepareDongle();
    ZigBeeStatus Initialize(ZigBeeDongleTiCc2531 dongle);
    void PermitNetworkJoining(bool permit);
    void RegisterSensorStateHandler(ISensorStateHandler sensorStateHandler);
    IEnumerable<OccupancySensor> GetSensors();
}