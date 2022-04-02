using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace link_r {
    public class ChangeOptionCommand : SystemCommand {
        private Module module;
        
        public ChangeOptionCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "configure-linkr";
        public override string helpsyntax => "<option> [value]";
        public override string description => "gets or sets Link/R options";
        public override string permissionnode => "commands.admin.configure.linkr";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            Server.ModuleConfig conf = server.GetModuleConfig(module);

            if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                if (arguments.Count > 1) {
                    if (arguments[0].Contains(".") && arguments[0].ToLower().Equals(arguments[0])) {
                        if (conf.Get(arguments[0]) != null) {
                            if (arguments[0] == "verification.memberrole" || arguments[0] == "verification.channel" || arguments[0] == "verification.postver.channel") {
                                if (Regex.Match(arguments[1], "^\\<\\#[0-9]+\\>$").Success) {
                                    arguments[1] = arguments[1].Substring(2).Remove(arguments[1].Length - 3);
                                } else if (Regex.Match(arguments[1], "^\\<\\@&[0-9]+\\>$").Success) {
                                    arguments[1] = arguments[1].Substring(3).Remove(arguments[1].Length - 4);
                                }

                                try {
                                    conf.Set(arguments[0], long.Parse(arguments[1]));
                                    await channel.SendMessageAsync("Configuration has been saved.");
                                } catch {
                                    if (arguments[0] == "verification.memberrole") {                                        
                                        await channel.SendMessageAsync("Invalid value, expected: role mention");
                                    } else if (arguments[0] == "verification.channel" || arguments[0] == "verification.postver.channel") {
                                        await channel.SendMessageAsync("Invalid value, expected: channel mention");
                                    } else {
                                        await channel.SendMessageAsync("Invalid value, expected: number");
                                    }
                                }
                            } else if (arguments[0] == "verification.nicknames.overridenickname" || arguments[0] == "verification.nicknames.usedisplayname") {
                                try {
                                    conf.Set(arguments[0], bool.Parse(arguments[1]));
                                    await channel.SendMessageAsync("Configuration has been saved.");
                                } catch {
                                    await channel.SendMessageAsync("Invalid value, expected: true/false");
                                }
                            } else {
                                conf.Set(arguments[0], arguments[1]);
                                await channel.SendMessageAsync("Configuration has been saved.");
                            }
                        } else {
                            await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n - verification.message.template\n - verification.memberrole\n - verification.channel\n - verification.channel.message.template\n - verification.nicknames.overridenickname\n - verification.nicknames.usedisplayname\n - verification.postver.channel\n - verification.postver.message.template");
                        }
                    } else {
                        await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n - verification.message.template\n - verification.memberrole\n - verification.channel\n - verification.channel.message.template\n - verification.nicknames.overridenickname\n - verification.nicknames.usedisplayname\n - verification.postver.channel\n - verification.postver.message.template");
                    }
                } else {
                    if (arguments.Count > 0 && arguments[0].Contains(".") && arguments[0].ToLower().Equals(arguments[0])) {
                        if (conf.Get(arguments[0]) != null) {
                            if (arguments[0] == "verification.memberrole") {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ": <@&" + conf.Get(arguments[0]) + ">");
                            } else if (arguments[0] == "verification.channel" || arguments[0] == "verification.postver.channel") {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ": <#" + conf.Get(arguments[0]) + ">");
                            } else if (arguments[0] == "verification.nicknames.overridenickname" || arguments[0] == "verification.nicknames.usedisplayname") {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ": ***" + conf.Get(arguments[0]) + "***");
                            } else {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ":\n```" + conf.Get(arguments[0]).ToString().Replace("```", "'''") + "```");
                            }
                        } else {
                            await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n - verification.message.template\n - verification.memberrole\n - verification.channel\n - verification.channel.message.template\n - verification.nicknames.overridenickname\n - verification.nicknames.usedisplayname\n - verification.postver.channel\n - verification.postver.message.template");
                        }
                    } else {
                        await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n - verification.message.template\n - verification.memberrole\n - verification.channel\n - verification.channel.message.template\n - verification.nicknames.overridenickname\n - verification.nicknames.usedisplayname\n - verification.postver.channel\n - verification.postver.message.template");
                    }
                }
            } else {
                await channel.SendMessageAsync("Link/R setup has not been completed, please run `setup-linkr` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
