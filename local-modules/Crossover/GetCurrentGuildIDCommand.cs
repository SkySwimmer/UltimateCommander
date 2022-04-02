using System.Collections.Generic;
using System.Threading.Tasks;
using CMDR;
using Discord.WebSocket;

namespace crossover {

    public class GetCurrentGuildIDCommand : SystemCommand
    {
        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("crossover", "Commands related to Crossover roles") };

        public override string commandid => "get-guild-id";
        public override string helpsyntax => "";
        public override string description => "shortcut for retrieving the guild ID";
        public override string permissionnode => "commands.admin.getguildid";

        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            await channel.SendMessageAsync("This server's guild ID: " + guild.Id);
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
        }
    }

}