using CMDR;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace levelup {
    public class ResetUserXPCommand : SystemCommand {
        private Module module;
        
        public ResetUserXPCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };
        public override string commandid => "reset-user-xp";
        public override string helpsyntax => "<user-mention>";
        public override string description => "resets the XP of a specific members in this server (only the XP of the current level)";
        public override string permissionnode => "commands.admin.levelup.reset.user.xp";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            if (arguments.Count >= 1) {
                ulong id = 0;
                if (Regex.Match(arguments[0], "^\\<\\@![0-9]+\\>$").Success) {
                    id = ulong.Parse(arguments[0].Substring(3).Remove(arguments[0].Length - 4));
                } else {
                    try {
                        id = ulong.Parse(arguments[0]);
                    } catch {
                        var en = guild.GetUsersAsync().GetAsyncEnumerator();
                        while (true) {
                            if (en.Current != null) {
                                foreach (IGuildUser usr in en.Current) {
                                    if (usr.DisplayName == arguments[0]) {
                                        id = usr.Id;
                                    }
                                }
                            }
                            if (!en.MoveNextAsync().GetAwaiter().GetResult())
                                break;
                        }
                        if (id == 0) {
                            en = guild.GetUsersAsync().GetAsyncEnumerator();
                            while (true) {
                                if (en.Current != null) {
                                    foreach (IGuildUser usr in en.Current) {
                                        if (usr.Nickname == arguments[0] && id == 0) {
                                            id = usr.Id;
                                        }
                                    }
                                }
                                if (!en.MoveNextAsync().GetAwaiter().GetResult())
                                    break;
                            }
                        }
                    }
                }

                if (id == 0) {
                    await channel.SendMessageAsync("Invalid usage, parameter 'user' is invalid.");
                } else {
                    Server server = GetBot().GetServerFromSocketGuild(guild);
                    ConfigDictionary<string, object> mem = module.serverMemory[server];
                    Server.ModuleConfig conf = server.GetModuleConfig(module);
                    SocketGuildUser usr = guild.GetUser(id);

                    if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                        if (conf.GetOrDefault("users", null) != null) {
                            List<ulong> users = Serializer.Deserialize<List<ulong>>(conf.GetOrDefault("users", null).ToString());
                            if (users.Contains(id)) {
                                int defaultLevelBaseXP = (int)conf.GetOrDefault("xp.levelup.base", 1400);
                                string uL = (string)conf.GetOrDefault("user-" + id, null);
                                if (uL == null) {
                                    uL = Serializer.Serialize(new Module.UserLevel() {
                                        LevelUpXP = defaultLevelBaseXP,
                                        Level = 1
                                    });
                                }
                                Module.UserLevel level = Serializer.Deserialize<Module.UserLevel>(uL);
                                level.TotalXP -= level.CurrentXP;
                                level.CurrentXP = 0;
                                conf.Set("user-" + id, Serializer.Serialize(level));
                                server.SaveAll();
                            }
                        }
                        await channel.SendMessageAsync("Success! Resetted XP of user `" + (usr.Nickname == null ? usr.DisplayName : usr.Nickname) + "`!");
                    } else {
                        await channel.SendMessageAsync("LevelUP setup has not been completed, please run `levelup-setup` first.");
                    }
                }
            } else {
                await channel.SendMessageAsync("Invalid usage, missing the 'user' parameter.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
