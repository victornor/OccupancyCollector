using System;
using Microsoft.Extensions.Logging;
using ZigBeeNet;

namespace OccupancyCollector.Services.Zigbee.Listeners;

public class CommandListener : IZigBeeCommandListener
{
    private readonly Action<ZigBeeCommand> _commandReceivedAction;

    public CommandListener(Action<ZigBeeCommand> commandReceivedAction)
        => _commandReceivedAction = commandReceivedAction;
    
    public void CommandReceived(ZigBeeCommand command)
        => _commandReceivedAction?.Invoke(command);
    
}