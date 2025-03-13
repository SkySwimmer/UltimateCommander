using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace link_r {
    public class CancelSetupCommand : SystemCommand {
        private Module module;
        
        public CancelSetupCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "cancel-linkr-setup";
        public override string helpsyntax => "";
        public override string description => "cancels a Link/R setup session";
        public override string permissionnode => "commands.admin.setup.linkr.cancel";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            ConfigDictionary<string, object> mem = module.serverMemory[server];

            if ((bool)mem.GetValueOrDefault("SetupRunning", false)) {
                mem.Put("SetupChannel", false);
                await channel.SendMessageAsync("Link/R setup cancelled.");
            } else {
                await channel.SendMessageAsync("Link/R setup is not running.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
