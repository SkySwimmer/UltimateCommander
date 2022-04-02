using CMDR;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace link_r {
    public class GetUserInfoCommand : SystemCommand {
        private Module module;
        
        public GetUserInfoCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "get-user-details";
        public override string helpsyntax => "<user>";
        public override string description => "retrieves details about a linked user in this server";
        public override string permissionnode => "commands.admin.linkr.getuserinfo";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            Server.ModuleConfig conf = server.GetModuleConfig(module);

            if ((bool)conf.GetOrDefault("SetupCompleted", false)) {                
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
                        if (conf.Get("user-" + id) == null) {
                            await channel.SendMessageAsync("**Error:** selected user has not been verified yet.");
                            return;
                        }
                        
                        SocketGuildUser userD = guild.GetUser(id);
                        Module.LinkedUser account = Serializer.Deserialize<Module.LinkedUser>(conf.Get("user-" + id).ToString());

                        await channel.SendMessageAsync("Details of verified user '" + (userD.Nickname == null ? userD.DisplayName : userD.Nickname) + "'\n```"
                                    + "Roblox user: " + account.username + "\n" 
                                    + "Roblox displayname: " + account.displayName + "\n"
                                    + "```");
                    }
                } else {
                    await channel.SendMessageAsync("Invalid usage, missing the 'user' parameter.");
                }
            } else {
                await channel.SendMessageAsync("Link/R setup has not been completed, please run `setup-linkr` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
