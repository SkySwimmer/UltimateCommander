using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMDR;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace crossover {

    public class RoleConfigurationCommand : SystemCommand
    {
        private Module module;
        
        public RoleConfigurationCommand(Module module) {
            this.module = module;
        }


        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("crossover", "Commands related to Crossover roles") };

        public override string commandid => "configure-crossover";
        public override string helpsyntax => "<list/add/remove> [<role-mention>] [<target-guild-id>] [<target-role>]";
        public override string description => "configures crossover roles";
        public override string permissionnode => "commands.admin.configure.crossover";

        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            Server serverData = GetBot().GetServerFromSocketGuild(guild);
            var conf = serverData.GetModuleConfig(module);
            ConfigDictionary<ulong, List<ulong>> roleConfig = module.DeserializeRoles(conf.GetOrDefault("roles", "<ConfigDictionary />").ToString());

            if (arguments.Count >= 1) {
                if (arguments[0] == "list") {
                    string msg = "";
                    foreach (ulong server in roleConfig.Keys) {
                        List<ulong> roles = roleConfig[server];
                        foreach (ulong role in roles) {
                            if (guild.GetRole(role) != null) {
                                if (msg == "") {
                                    string srv = "";
                                    if (GetBot().client.GetGuild(server) == null) {
                                        srv = "*[Unrecognized server: " + server + "]*";
                                    } else {
                                        srv = GetBot().client.GetGuild(server).Name;
                                    }
                                    msg = "**List of crossover roles:**\n - <@&" + role + ">: " + srv;
                                } else {
                                    string srv = "";
                                    if (GetBot().client.GetGuild(server) == null) {
                                        srv = "*[Unrecognized server: " + server + "]*";
                                    } else {
                                        srv = GetBot().client.GetGuild(server).Name;
                                    }
                                    msg += "\n - <@&" + role + ">: " + srv;
                                }
                            } else {
                                if (msg == "") {
                                    string srv = "";
                                    if (GetBot().client.GetGuild(server) == null) {
                                        srv = "*[Unrecognized server: " + server + "]*";
                                    } else {
                                        srv = GetBot().client.GetGuild(server).Name;
                                    }
                                    msg = "**List of crossover roles:**\n - *[Unrecognized role: <<" + role + ">>]*:" + srv;
                                } else {
                                    string srv = "";
                                    if (GetBot().client.GetGuild(server) == null) {
                                        srv = "*[Unrecognized server: " + server + "]*";
                                    } else {
                                        srv = GetBot().client.GetGuild(server).Name;
                                    }
                                    msg += "\n - *[Unrecognized role: <<" + role + ">>]*: " + srv;
                                }
                            }
                        }
                    }

                    if (msg == "") {
                        await channel.SendMessageAsync("**Error:** no crossover roles have been set up yet.");
                    } else {
                        await channel.SendMessageAsync(msg);
                    }
                } else if (arguments[0] == "add") {
                    if (arguments.Count < 3) {
                        if (arguments.Count == 2) {
                            await channel.SendMessageAsync("**Error:** missing the 'target-guild-id' parameter.");
                        } else {
                            await channel.SendMessageAsync("**Error:** missing the 'role-mention' parameter.");
                        }
                    } else {
                        ulong roleID = 0;
                        if (Regex.Match(arguments[1], "^\\<\\@&[0-9]+\\>$").Success) {
                            ulong id = ulong.Parse(arguments[1].Substring(3).Remove(arguments[1].Length - 4));
                            roleID = id;
                        } else {
                            foreach (SocketRole r in guild.Roles) {
                                if (r.Name == arguments[1]) {
                                    roleID = r.Id;
                                    break;
                                }
                            }
                        }

                        bool found = false;
                        if (roleID != 0) {
                            foreach (SocketRole r in guild.Roles) {
                                if (r.Id == roleID) {
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found) {
                            await channel.SendMessageAsync("**Error:** invalid value for 'role-mention', expected: role mention");
                        } else {
                            ulong server = 0;
                            if (Regex.Match(arguments[2], "^[0-9]+$").Success) {
                                try {
                                    server = ulong.Parse(arguments[2]);
                                } catch {
                                    await channel.SendMessageAsync("**Error:** invalid value for 'target-server-id', expected: guild id number");
                                    return;
                                }
                            } else {
                                await channel.SendMessageAsync("**Error:** invalid value for 'target-server-id', expected: guild id number");
                                return;
                            }

                            SocketGuild g = GetBot().client.GetGuild(server);
                            if (g == null) {
                                await channel.SendMessageAsync("**Error:** unable to access that server, please note that this bot needs to be present in the target server in order to create a crossover role.");
                                return;
                            } else {
                                ulong role = 0;
                                if (arguments.Count >= 4) {
                                    ulong tRoleID = 0;
                                    foreach (SocketRole r in g.Roles) {
                                        if (r.Name == arguments[3] || r.Id.ToString() == arguments[3]) {
                                            tRoleID = r.Id;
                                            break;
                                        }
                                    }

                                    found = false;
                                    if (tRoleID != 0) {
                                        foreach (SocketRole r in g.Roles) {
                                            if (r.Id == tRoleID) {
                                                found = true;
                                                break;
                                            }
                                        }
                                    }

                                    if (!found) {
                                        await channel.SendMessageAsync("**Error:** invalid value for 'target-role', expected: role name or ID");
                                        return;
                                    }

                                    role = tRoleID;
                                }

                                if (!roleConfig.ContainsKey(server)) {
                                    roleConfig[server] = new List<ulong>();
                                }
                                if (roleConfig[server].Contains(roleID)) {
                                    await channel.SendMessageAsync("**Error:** that role has already been registered.");
                                    return;
                                }

                                roleConfig[server].Add(roleID);
                                ConfigDictionary<string, ulong> filter = Serializer.Deserialize<ConfigDictionary<string, ulong>>(conf.GetOrDefault("roleFilters", "<ConfigDictionary />").ToString());
                                filter[server + "-" + roleID] = role;
                                conf.Set("roleFilters", Serializer.Serialize(filter));

                                conf.Set("roles", module.SerializeRoles(roleConfig));
                                await channel.SendMessageAsync("Saving configuration...");
                                serverData.SaveAll();
                                module.loadServer(serverData);
                                await channel.SendMessageAsync("**Success!** Added crossover role <@&" + roleID + "> and reloaded the server configuration!");
                            }
                        }
                    }
                } else if (arguments[0] == "remove") {
                    if (arguments.Count < 2) {
                        await channel.SendMessageAsync("**Error:** missing the 'role-mention' parameter.");
                    } else {
                        ulong roleID = 0;
                        if (Regex.Match(arguments[1], "^\\<\\@&[0-9]+\\>$").Success) {
                            ulong id = ulong.Parse(arguments[1].Substring(3).Remove(arguments[1].Length - 4));
                            roleID = id;
                        } else {
                            foreach (SocketRole r in guild.Roles) {
                                if (r.Name == arguments[1]) {
                                    roleID = r.Id;
                                    break;
                                }
                            }
                        }

                        foreach (ulong server in roleConfig.Keys) {
                            List<ulong> roles = roleConfig[server];
                            if (roles.Contains(roleID)) {
                                roles.Remove(roleID);

                                ConfigDictionary<string, ulong> filter = Serializer.Deserialize<ConfigDictionary<string, ulong>>(conf.GetOrDefault("roleFilters", "<ConfigDictionary />").ToString());
                                if (filter.ContainsKey(server + "-" + roleID))
                                    filter.Remove(server + "-" + roleID);
                                conf.Set("roleFilters", Serializer.Serialize(filter));

                                conf.Set("roles", module.SerializeRoles(roleConfig));
                                await channel.SendMessageAsync("Saving configuration...");
                                serverData.SaveAll();
                                var e = guild.GetUsersAsync().GetAsyncEnumerator();
                                while (true) {
                                    if (e.Current != null) {
                                        foreach (IUser usr in e.Current) 
                                            if (usr is SocketGuildUser) {{
                                                SocketGuildUser usrD = (SocketGuildUser)usr;
                                                if (usrD.Roles.FirstOrDefault(t => t.Id == roleID, null) != null) {
                                                    try {
                                                        usrD.RemoveRoleAsync(roleID).GetAwaiter().GetResult();
                                                    } catch {}
                                                }
                                            }
                                        }
                                    }
                                    if (!e.MoveNextAsync().GetAwaiter().GetResult())                                      
                                        break;
                                }

                                module.loadServer(serverData);
                                await channel.SendMessageAsync("**Success!** Removed crossover role <@&" + roleID + "> and reloaded the server configuration!");
                                return;
                            }
                        }
                        await channel.SendMessageAsync("**Error:** specified role has not been registered.");
                    }
                } else {
                    await channel.SendMessageAsync("**Error:** unrecognized configuration command.");
                }
            } else {
                await channel.SendMessageAsync("**Error:** missing the 'command' parameter.");
            }
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
        }
    }

}