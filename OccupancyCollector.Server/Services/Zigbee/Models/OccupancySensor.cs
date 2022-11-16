using System;
using Newtonsoft.Json;

namespace OccupancyCollector.Services.Zigbee.Models;

public class OccupancySensor
{
    public string Id { get; set; }
    public int Illuminance { get; set; }
    public bool Occupied { get; set; }
    public DateTime LastUpdate { get; set; }

    public OccupancySensor(string id, int illuminance, bool occupied)
        => (Id, Illuminance, Occupied) = (id, illuminance, occupied);

    public override string ToString()
        => JsonConvert.SerializeObject(this);
}