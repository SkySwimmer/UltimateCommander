using Discord;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMDR;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace permissionmanager
{
    public class PermissionManagerCommand : SystemCommand
    {
        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("permissions", "Permission manager commands") };

        public override string commandid => "permissionmanager";
        public override string helpsyntax => "<add/remove/list> <role-mention> [permission] [true/false]";
        public override string description => "configures permissions in this server";
        public override string permissionnode => "commands.admin.permissions.manage";

        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            if (arguments.Count >= 2) {
                ulong roleId = 0;
                if (Regex.Match(arguments[1], "^\\<\\@&[0-9]+\\>$").Success) {
                    roleId = ulong.Parse(arguments[1].Substring(3).Remove(arguments[1].Length - 4));
                } else {
                    try {
                        roleId = ulong.Parse(arguments[1]);
                    } catch {
                        foreach (SocketRole role in guild.Roles) {
                            if (role.Name == arguments[1]) {
                                roleId = role.Id;
                                break;
                            }
                        }
                    }
                }
                if (guild.GetRole(roleId) == null) {
                    await channel.SendMessageAsync("**Error:** invalid value for parameter 'role', expected: role mention");
                    return;
                }
                if (roleId != 0) {
                    Server srv = GetBot().GetServerFromSocketGuild(guild);
                    Role roleInfo = srv.GetRole(roleId);
                    if (arguments[0] == "list") {
                        string msg = "List of permissions for role <@&" + roleId + ">:\n";
                        foreach (string perm in roleInfo.permissions) {
                            msg += " - `" + perm + "`\n";
                        }
                        foreach (string perm in roleInfo.permissionsblacklist) {
                            msg += " - `" + perm + "` (denied)\n";
                        }
                        await channel.SendMessageAsync(msg);
                    } else if (arguments[0] == "add") {
                        if (arguments.Count < 3) {
                            await channel.SendMessageAsync("**Error:** missing parameter 'permission'");
                            return;
                        }

                        bool allow = true;
                        if (arguments.Count >= 4) {
                            try {
                                allow = bool.Parse(arguments[3]);
                            } catch {
                                await channel.SendMessageAsync("**Error:** invalid value for parameter 'is-whitelisted-permission', expected: true/false");
                                return;
                            }
                        }

                        if (allow && !roleInfo.permissions.Contains(arguments[2])) {
                            roleInfo.permissions.Add(arguments[2]);
                        } else if (!roleInfo.permissionsblacklist.Contains(arguments[2])) {
                            if (arguments[2].ToLower() == "sys.anyone") {
                                await channel.SendMessageAsync("**Error:** the permission sys.anyone may not be denied!");
                                return;
                            }
                            roleInfo.permissionsblacklist.Add(arguments[2]);
                        }
                        srv.SaveAll(true);
                        await channel.SendMessageAsync("Success! Permission configuration has been updated!");
                    } else if (arguments[0] == "remove") {
                        if (arguments.Count < 3) {
                            await channel.SendMessageAsync("**Error:** missing parameter 'permission'");
                            return;
                        }

                        bool allow = true;
                        if (arguments.Count >= 4) {
                            try {
                                allow = bool.Parse(arguments[3]);
                            } catch {
                                await channel.SendMessageAsync("**Error:** invalid value for parameter 'is-whitelisted-permission', expected: true/false");
                                return;
                            }
                        }

                        if (arguments[2].ToLower() == "sys.anyone") {
                            await channel.SendMessageAsync("**Error:** the permission sys.anyone may not be removed!");
                            return;
                        }

                        bool found = false;
                        if (allow && roleInfo.permissions.Contains(arguments[2])) {
                            roleInfo.permissions.Remove(arguments[2]);
                            found = true;
                        } else if (roleInfo.permissionsblacklist.Contains(arguments[2])) {
                            roleInfo.permissionsblacklist.Remove(arguments[2]);
                            found = true;
                        }
                        if (found) {
                            srv.SaveAll(true);
                            await channel.SendMessageAsync("Success! Permission configuration has been updated!");
                        } else {
                            if (arguments.Count >= 4) {
                                if (allow) {
                                    await channel.SendMessageAsync("**Error:** could not find the specified permission in the permission whitelist");
                                } else {
                                    await channel.SendMessageAsync("**Error:** could not find the specified permission in the permission blacklist");
                                }
                            } else {
                                await channel.SendMessageAsync("**Error:** could not find the specified permission in the permission whitelist or blacklist");
                            }
                        }
                    } else {
                        await channel.SendMessageAsync("**Error:** invalid value for parameter 'command', expected: add/remove/list");
                    }
                } else {
                    await channel.SendMessageAsync("**Error:** invalid value for parameter 'role', expected: role mention");
                }
            } else {
                if (arguments.Count == 0) {
                    await channel.SendMessageAsync("**Error:** missing parameter 'command'");
                } else {
                    await channel.SendMessageAsync("**Error:** missing parameter 'role'");
                }
            }
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
        }
    }
}