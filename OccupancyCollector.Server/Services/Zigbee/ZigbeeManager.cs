using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using zig.Configuration;
using ZigBeeNet;
using ZigBeeNet.App.Basic;
using ZigBeeNet.App.Discovery;
using ZigBeeNet.App.IasClient;
using ZigBeeNet.Hardware.TI.CC2531;
using ZigBeeNet.Tranport.SerialPort;
using ZigBeeNet.Transport;

namespace OccupancyCollector.Services.Zigbee;

public class ZigbeeManager
{
    private ILogger<ZigbeeManager> _logger;
    private Configuration.Configuration _configuration;

    private ZigBeeNetworkManager _networkManager;

    public ZigbeeManager(ILogger<ZigbeeManager> logger, Configuration.Configuration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InitializeAsync()
    {
        var port = new ZigBeeSerialPort(_configuration.Port, 115200, FlowControl.FLOWCONTROL_OUT_NONE);
        var dongle = new ZigBeeDongleTiCc2531(port);

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

        
        
        _networkManager.Initialize();
    }
    
}