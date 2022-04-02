using CMDR;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace levelup {
    public class ResetUserLevelCommand : SystemCommand {
        private Module module;
        
        public ResetUserLevelCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };
        public override string commandid => "reset-user-level";
        public override string helpsyntax => "<user-mention>";
        public override string description => "resets the level of a specific members in this server";
        public override string permissionnode => "commands.admin.levelup.reset.user.level";
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
                                users.Remove(id);
                                conf.Set("user-" + id, null);
                            }
                            conf.Set("users", Serializer.Serialize(users));
                            server.SaveAll();
                        }
                        if (conf.GetOrDefault("levelroles", null) != null) {
                            ConfigDictionary<int, ulong> levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());

                            SocketRole lastRole = null;
                            foreach (ulong r in levelRoles.Values) {
                                SocketRole role = guild.GetRole(r);
                                if (role != null) {
                                    lastRole = role;
                                    if (usr != null) {
                                        try {
                                            usr.RemoveRoleAsync(role.Id).GetAwaiter().GetResult();
                                        } catch {
                                        }
                                    }
                                }
                            }
                        }
                        await channel.SendMessageAsync("Success! Resetted the level of user `" + (usr.Nickname == null ? usr.DisplayName : usr.Nickname) + "`!");
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
