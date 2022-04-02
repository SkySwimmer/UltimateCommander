using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMDR;
using Discord;
using Discord.WebSocket;

namespace levelup {
    public class SetUserLevelCommand : SystemCommand
    {
        private Module module;
        
        public SetUserLevelCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };

        public override string commandid => "set-user-level";
        public override string helpsyntax => "<user-mention> <level>";
        public override string description => "sets the level of a specific member";
        public override string permissionnode => "commands.admin.levelup.set.user.level";

        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            if (arguments.Count >= 2) {
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
                        int nlevel = 0;
                        try {
                            nlevel = int.Parse(arguments[1]);
                            int maxLevel = (int)conf.GetOrDefault("xp.maxlevel", 1000);
                            if (nlevel < 1 || nlevel > maxLevel) {
                                await channel.SendMessageAsync("Invalid usage, parameter 'level' is invalid.");
                                return;
                            }
                        } catch {
                            await channel.SendMessageAsync("Invalid usage, parameter 'level' is invalid.");
                            return;
                        }
                        int defaultLevelBaseXP = (int)conf.GetOrDefault("xp.levelup.base", 1400);
                        Module.UserLevel level = new Module.UserLevel();
                        level.CurrentXP = 0;
                        level.TotalXP = 0;
                        level.Level = 1;
                        level.LevelUpXP = defaultLevelBaseXP;
                        while (level.Level < nlevel) {
                            level.TotalXP += 1;
                            level.CurrentXP += 1;
                            
                            while (level.CurrentXP > level.LevelUpXP) {
                                level.Level++;
                                level.CurrentXP -= level.LevelUpXP;
                                level.LevelUpXP = (level.LevelUpXP * 2) + (level.LevelUpXP / 4);
                            }
                        }
                        if (conf.GetOrDefault("levelroles", null) != null) {
                            ConfigDictionary<int, ulong> levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());

                            SocketRole lastRole = null;
                            for (int i = 0; i <= level.Level; i++) {
                                if (levelRoles.ContainsKey(i)) {
                                    SocketRole role = guild.GetRole(levelRoles[i]);
                                    if (role != null) {
                                        lastRole = role;
                                        if (usr != null) {
                                            try {
                                                usr.AddRoleAsync(role.Id).GetAwaiter().GetResult();
                                            } catch {
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        conf.Set("user-" + id, Serializer.Serialize(level));
                        await channel.SendMessageAsync("Success! User level has been changed to " + level.Level + "!");
                    } else {
                        await channel.SendMessageAsync("LevelUP setup has not been completed, please run `levelup-setup` first.");
                    }
                }
            } else {
                if (arguments.Count == 0) {
                    await channel.SendMessageAsync("Invalid usage, missing the 'user' parameter.");
                } else {
                    await channel.SendMessageAsync("Invalid usage, missing the 'level' parameter.");
                }
            }
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
        }
    }
}