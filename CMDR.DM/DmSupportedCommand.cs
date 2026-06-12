using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CMDR
{
    public interface DmSupportedCommand
    {
        public Task OnExecuteFromDM(SocketUser user, SocketDMChannel channel, SocketSlashCommand ev, string fullmessage, string arguments_string, List<string> arguments);

    }
}