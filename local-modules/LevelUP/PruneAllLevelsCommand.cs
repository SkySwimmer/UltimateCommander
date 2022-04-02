using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace levelup {
    public class PruneAllLevelsCommand : SystemCommand {
        private Module module;
        
        public PruneAllLevelsCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };
        public override string commandid => "prune-all-levels";
        public override string helpsyntax => "";
        public override string description => "resets all levels of all members in this server";
        public override string permissionnode => "commands.admin.levelup.prune.all";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            ConfigDictionary<string, object> mem = module.serverMemory[server];
            Server.ModuleConfig conf = server.GetModuleConfig(module);

            if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                if (conf.GetOrDefault("users", null) != null) {
                    List<ulong> users = Serializer.Deserialize<List<ulong>>(conf.GetOrDefault("users", null).ToString());
                    int defaultLevelBaseXP = (int)conf.GetOrDefault("xp.levelup.base", 1400);
                    
                    foreach (ulong usr in users) {
                        conf.Set("user-" + usr, null);
                    }
                    
                    users.Clear();
                    conf.Set("users", Serializer.Serialize(users));
                    server.SaveAll();
                }
                await channel.SendMessageAsync("Success! All levels have been pruned!");
            } else {
                await channel.SendMessageAsync("LevelUP setup has not been completed, please run `levelup-setup` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
