using OccupancyCollector.Services.Zigbee.Models;

namespace OccupancyCollector.Services.Zigbee.StateHandlers.Abstractions;

public interface ISensorStateHandler
{
    void HandleSensorState(OccupancySensor sensorState);
}