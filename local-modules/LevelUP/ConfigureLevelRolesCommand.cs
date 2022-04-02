using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace levelup {
    public class ConfigureLevelRolesCommand : SystemCommand {
        private Module module;
        
        public ConfigureLevelRolesCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };
        public override string commandid => "configure-level-roles";
        public override string helpsyntax => "<list/add/remove> [level] [role-mention]";
        public override string description => "gets or sets LevelUP options";
        public override string permissionnode => "commands.admin.configure.levelup.roles";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            Server.ModuleConfig conf = server.GetModuleConfig(module);

            if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                if (arguments.Count >= 1) {
                    if (arguments[0] == "list") {
                        ConfigDictionary<int, ulong> levelRoles = new ConfigDictionary<int, ulong>();
                        if (conf.GetOrDefault("levelroles", null) != null) {
                            levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());
                        }
                        if (levelRoles.Count == 0) {
                            await channel.SendMessageAsync("Role configuration is currently empty");
                        } else {
                            string msg = "Level role configuration:\n";
                            foreach (int lv in levelRoles.Keys.OrderBy(t => t)) {
                                ulong r = levelRoles[lv];
                                SocketRole role = guild.GetRole(r);
                                if (role != null) {
                                    msg += " - Level " + lv + ": `" + role.Name + "`\n";
                                }
                            }
                            await channel.SendMessageAsync(msg);
                        }
                    } else if (arguments[0] == "remove") {
                        ConfigDictionary<int, ulong> levelRoles = new ConfigDictionary<int, ulong>();
                        if (conf.GetOrDefault("levelroles", null) != null) {
                            levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());
                        }
                        if (levelRoles.Count == 0) {
                            await channel.SendMessageAsync("**ERROR:** Role configuration is currently empty");
                        } else {
                            try {
                                int lv = int.Parse(arguments[1]);
                                if (levelRoles.ContainsKey(lv)) {
                                    levelRoles.Remove(lv);
                                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("levelroles", Serializer.Serialize(levelRoles));
                                    await channel.SendMessageAsync("Configuration has been saved.");
                                } else {
                                    await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'level'");
                                }
                            } catch {
                                await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'level'");
                            }
                        }
                    } else if (arguments[0] == "add") {
                        ConfigDictionary<int, ulong> levelRoles = new ConfigDictionary<int, ulong>();
                        if (conf.GetOrDefault("levelroles", null) != null) {
                            levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());
                        }
                        
                        if (arguments.Count >= 3) {
                            ulong roleID = 0;
                            if (Regex.Match(arguments[2], "^\\<\\@&[0-9]+\\>$").Success) {
                                ulong id = ulong.Parse(arguments[1].Substring(3).Remove(arguments[1].Length - 4));
                                roleID = id;
                            } else {
                                foreach (SocketRole r in guild.Roles) {
                                    if (r.Name == arguments[2]) {
                                        roleID = r.Id;
                                        break;
                                    }
                                }
                            }

                            if (roleID == 0) {
                                await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'role'");
                            } else {
                                SocketRole r = guild.GetRole(roleID);
                                if (r == null) {
                                    await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'role'");
                                } else {
                                    try {
                                        int lv = int.Parse(arguments[1]);
                                        levelRoles[lv] = roleID;
                                        server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("levelroles", Serializer.Serialize(levelRoles));
                                        await channel.SendMessageAsync("Configuration has been saved.");
                                    } catch {
                                        await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'level'");
                                    }
                                }
                            }
                        } else {
                            if (arguments.Count == 1) {
                                await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'level'");
                            } else {
                                await channel.SendMessageAsync("**ERROR:** Invalid value for parameter 'role'");
                            }
                        }
                    } else {
                        await channel.SendMessageAsync("Unknown configuration command: " + arguments[0]);
                    }
                } else {
                    await channel.SendMessageAsync("Please specify a configuration command.");    
                }
            } else {
                await channel.SendMessageAsync("LevelUP setup has not been completed, please run `levelup-setup` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
