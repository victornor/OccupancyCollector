using OccupancyCollector.Services.Zigbee.Models;
using OccupancyCollector.Services.Zigbee.StateHandlers.Abstractions;
using Serilog;

namespace OccupancyCollector.Services.Zigbee.StateHandlers;

public class ConsoleStateHandler : ISensorStateHandler
{
    public void HandleSensorState(OccupancySensor sensorState)
    {
        Log.Information($"Updated sensor state: {sensorState}");
    }
}