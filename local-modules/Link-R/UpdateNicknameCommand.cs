using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

namespace link_r {
    public class UpdateNicknameCommand : SystemCommand {
        private Module module;
        
        public UpdateNicknameCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "update-nickname";
        public override string helpsyntax => "";
        public override string description => "updates your server nickname (for roblox display names)";
        public override string permissionnode => "sys.anyone";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser usr, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            ConfigDictionary<string, object> mem = module.serverMemory[server];
            Server.ModuleConfig conf = server.GetModuleConfig(module);

            if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                if ((int)mem.GetValueOrDefault("cooldown-change-nickname-" + usr.Id, 0) == 0) {
                    if ((bool)conf.Get("verification.nicknames.overridenickname")) {
                        try {
                            if ((bool)conf.Get("verification.nicknames.usedisplayname")) {
                                if (conf.Get("user-" + usr.Id) == null) {
                                    await channel.SendMessageAsync("Your account has not yet been verified, please verify your user before running this command.");
                                    return;
                                }

                                SocketGuildUser member = (SocketGuildUser)usr;
                                ulong account = Serializer.Deserialize<Module.LinkedUser>(conf.Get("user-" + usr.Id).ToString()).robloxUserId;

                                string json = new HttpClient().GetStringAsync("https://users.roblox.com/v1/users/" + account).GetAwaiter().GetResult();
                                Module.UserInfo info = JsonConvert.DeserializeObject<Module.UserInfo>(json);

                                mem.Put("cooldown-change-nickname-" + usr.Id, 60);
                                new Thread(() => {
                                    while ((int)mem.GetValueOrDefault("cooldown-change-nickname-" + usr.Id, 0) > 0) {
                                        mem.Put("cooldown-change-nickname-" + usr.Id, (int)mem.GetValueOrDefault("cooldown-change-nickname-" + usr.Id, 0) - 1);
                                        Thread.Sleep(1000);
                                    }
                                }).Start();

                                Module.LinkedUser user = new Module.LinkedUser();
                                user.discordID = usr.Id;
                                user.robloxUserId = account;
                                user.username = info.name;
                                user.displayName = info.displayName;
                                if (user.displayName == null)
                                    user.displayName = info.name;
                                conf.Set("user-" + usr.Id, Serializer.Serialize(user));

                                member.ModifyAsync(t => t.Nickname = user.displayName).GetAwaiter().GetResult();
                                await channel.SendMessageAsync("Success! Your nickname has been updated!");
                                return;
                            }
                        } catch {
                            await channel.SendMessageAsync("An error occured while attempting to update your nickname, please try again later.");
                            return;
                        }
                    }

                    await channel.SendMessageAsync("This server does not support updating your nickname.");
                } else {
                    await channel.SendMessageAsync("This command is on cooldown, please wait " + (int)mem.GetValueOrDefault("cooldown-change-nickname-" + usr.Id, 0) + " more seconds before trying again.");
                }
            } else {
                await channel.SendMessageAsync("Link/R setup has not been completed, inform a admin that they need run `setup-linkr` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
