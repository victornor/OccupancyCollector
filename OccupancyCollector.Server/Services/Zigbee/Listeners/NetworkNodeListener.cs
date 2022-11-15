using System;
using ZigBeeNet;

namespace OccupancyCollector.Services.Zigbee.Listeners;

public class NetworkNodeListener : IZigBeeNetworkNodeListener
{
    private readonly Action<ZigBeeNode> _nodeAddedAction;
    private readonly Action<ZigBeeNode> _nodeRemovedAction;
    private readonly Action<ZigBeeNode> _nodeUpdatedAction;

    public NetworkNodeListener(Action<ZigBeeNode> nodeAddedAction, Action<ZigBeeNode> nodeRemovedAction,
        Action<ZigBeeNode> nodeUpdatedAction)
        => (_nodeAddedAction, _nodeRemovedAction, _nodeUpdatedAction) =
            (nodeAddedAction, nodeRemovedAction, nodeUpdatedAction);

    public void NodeAdded(ZigBeeNode node)
        => _nodeAddedAction?.Invoke(node);

    public void NodeRemoved(ZigBeeNode node)
        => _nodeRemovedAction?.Invoke(node);

    public void NodeUpdated(ZigBeeNode node)
        => _nodeUpdatedAction?.Invoke(node);
}