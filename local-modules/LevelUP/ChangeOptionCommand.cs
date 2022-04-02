using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace levelup {
    public class ChangeOptionCommand : SystemCommand {
        private Module module;
        
        public ChangeOptionCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };
        public override string commandid => "configure-levelup";
        public override string helpsyntax => "<option> [value]";
        public override string description => "gets or sets LevelUP options";
        public override string permissionnode => "commands.admin.configure.levelup";
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
                            if (conf.Get(arguments[0]) is int) {
                                try {
                                    int val = int.Parse(arguments[1]);
                                    if (arguments[0] == "xp.maxlevel") {
                                        if (val < 2) {
                                            await channel.SendMessageAsync("Invalid value, expected: number (greater than one)");
                                            return;        
                                        } else if (val > int.MaxValue / 1000) {
                                            await channel.SendMessageAsync("Invalid value, expected: number (less than " + (int.MaxValue / 1000) + ")");
                                        }
                                    }

                                    conf.Set(arguments[0], val);
                                    await channel.SendMessageAsync("Configuration has been saved.");
                                } catch {
                                    await channel.SendMessageAsync("Invalid value, expected: number.");
                                }
                            } else if (conf.Get(arguments[0]) is ulong) {
                                if (Regex.Match(arguments[1], "^\\<\\#[0-9]+\\>$").Success) {
                                    arguments[1] = arguments[1].Substring(2).Remove(arguments[1].Length - 3);
                                }

                                try {
                                    conf.Set(arguments[0], ulong.Parse(arguments[1]));
                                    await channel.SendMessageAsync("Configuration has been saved.");
                                } catch {
                                    await channel.SendMessageAsync("Invalid value, expected: channel mention.");
                                }
                            } else if (conf.Get(arguments[0]) is bool) {
                                try {
                                    conf.Set(arguments[0], bool.Parse(arguments[1]));
                                    await channel.SendMessageAsync("Configuration has been saved.");
                                } catch {
                                    await channel.SendMessageAsync("Invalid value, expected: true or false.");
                                }
                            } else {
                                conf.Set(arguments[0], arguments[1]);
                                await channel.SendMessageAsync("Configuration has been saved.");
                            }
                        } else {
                            await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n"
                                    + " - xp.maxlevel: max level"
                                    + " - xp.increase.max: XP character limit\n" 
                                    + " - xp.increase.multiplier.max: XP multiplier\n"
                                    + " - xp.levelup.base: XP needed for first advancement\n"
                                    + " - messages.sendonadvancement: send a message on advancement [true/false]\n"
                                    + " - messages.advancement.usecurrentchannel: send advancement messages in current channel [true/false]\n"
                                    + " - messages.advancement.channel: channel to send advancement messages to (optional)\n"
                                    + " - messages.advancement.template: advancement message template\n"
                                    + " - messages.sendonroleadvancement: send a message on role advancement [true/false]\n"
                                    + " - messages.roleadvancement.usecurrentchannel: send role advancement messages in current channel [true/false]\n"
                                    + " - messages.roleadvancement.channel: channel to send role advancement messages to (optional)\n"
                                    + " - messages.roleadvancement.template: role advancement message template\n"
                                    + "\n"
                                    + "Note: role configuration cannot be done using this command, use `configure-level-roles` to configure level roles");
                        }
                    } else {
                        await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n - verification.message.template\n - verification.memberrole\n - verification.channel\n - verification.channel.message.template\n - verification.nicknames.overridenickname\n - verification.nicknames.usedisplayname\n - verification.postver.channel\n - verification.postver.message.template");
                    }
                } else {
                    if (arguments.Count > 0 && arguments[0].Contains(".") && arguments[0].ToLower().Equals(arguments[0])) {
                        if (conf.Get(arguments[0]) != null) {
                            if (arguments[0] == "messages.advancement.channel" || arguments[0] == "messages.roleadvancement.channel") {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ": <#" + conf.Get(arguments[0]) + ">");
                            } else if (!arguments[0].EndsWith("template")) {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ": **" + conf.Get(arguments[0]) + "**");
                            } else {
                                await channel.SendMessageAsync("Value of " + arguments[0] + ":\n```" + conf.Get(arguments[0]).ToString().Replace("```", "'''") + "```");
                            }
                        } else {
                            await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n"
                                    + " - xp.maxlevel: max level"
                                    + " - xp.increase.max: XP character limit\n" 
                                    + " - xp.increase.multiplier.max: XP multiplier\n"
                                    + " - xp.levelup.base: XP needed for first advancement\n"
                                    + " - messages.sendonadvancement: send a message on advancement [true/false]\n"
                                    + " - messages.advancement.usecurrentchannel: send advancement messages in current channel [true/false]\n"
                                    + " - messages.advancement.channel: channel to send advancement messages to (optional)\n"
                                    + " - messages.advancement.template: advancement message template\n"
                                    + " - messages.sendonroleadvancement: send a message on role advancement [true/false]\n"
                                    + " - messages.roleadvancement.usecurrentchannel: send role advancement messages in current channel [true/false]\n"
                                    + " - messages.roleadvancement.channel: channel to send role advancement messages to (optional)\n"
                                    + " - messages.roleadvancement.template: role advancement message template\n"
                                    + "\n"
                                    + "Note: role configuration cannot be done using this command, use `configure-level-roles` to configure level roles");
                        }
                    } else {
                        await channel.SendMessageAsync("Invalid configuration option name.\n\nSupported option names:\n"
                                + " - xp.maxlevel: max level"
                                + " - xp.increase.max: XP character limit\n" 
                                + " - xp.increase.multiplier.max: XP multiplier\n"
                                + " - xp.levelup.base: XP needed for first advancement\n"
                                + " - messages.sendonadvancement: send a message on advancement [true/false]\n"
                                + " - messages.advancement.usecurrentchannel: send advancement messages in current channel [true/false]\n"
                                + " - messages.advancement.channel: channel to send advancement messages to (optional)\n"
                                + " - messages.advancement.template: advancement message template\n"
                                + " - messages.sendonroleadvancement: send a message on role advancement [true/false]\n"
                                + " - messages.roleadvancement.usecurrentchannel: send role advancement messages in current channel [true/false]\n"
                                + " - messages.roleadvancement.channel: channel to send role advancement messages to (optional)\n"
                                + " - messages.roleadvancement.template: role advancement message template\n"
                                + "\n"
                                + "Note: role configuration cannot be done using this command, use `configure-level-roles` to configure level roles");
                    }
                }
            } else {
                await channel.SendMessageAsync("LevelUP setup has not been completed, please run `levelup-setup` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
