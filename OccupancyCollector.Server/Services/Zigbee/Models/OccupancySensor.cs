using System;

namespace OccupancyCollector.Services.Zigbee.Models;

public class OccupancySensor
{
    public string Id { get; set; }
    public int Illuminance { get; set; }
    public bool Occupied
    {
        get => LastUpdate > DateTime.Now.AddSeconds(-10) && _occupied;
        set => _occupied = value;
    }
    private bool _occupied;
    
    public DateTime LastUpdate { get; set; }

    public OccupancySensor(string id, int illuminance, bool occupied)
        => (Id, Illuminance, _occupied) = (id, illuminance, occupied);
}