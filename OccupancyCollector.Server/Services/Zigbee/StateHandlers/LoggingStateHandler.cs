using System;
using System.IO;
using Newtonsoft.Json;
using OccupancyCollector.Services.Zigbee.Models;
using OccupancyCollector.Services.Zigbee.StateHandlers.Abstractions;

namespace OccupancyCollector.Services.Zigbee.StateHandlers;

public class LoggingStateHandler : ISensorStateHandler
{
    private string _file;

    public LoggingStateHandler(string file)
        => _file = file;
    
    public void HandleSensorState(OccupancySensor sensorState)
    {
        File.AppendAllLines(_file,
            new[] { $"{DateTime.Now.ToLongTimeString()}: {JsonConvert.SerializeObject(sensorState)}" });
    }
}