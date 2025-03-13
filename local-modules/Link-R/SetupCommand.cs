using CMDR;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace link_r {
    public class SetupCommand : SystemCommand {
        private static Random rnd = new Random();
        private Module module;
        public delegate void Handler(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem);

        public SetupCommand(Module module) {
            this.module = module;
            module.GetBot().client.MessageReceived += (message) => {
                if (!(message.Channel is SocketTextChannel))
                    return Task.CompletedTask;

                SocketTextChannel channel = (SocketTextChannel)message.Channel;
                SocketGuild guild = channel.Guild;
                SocketGuildUser user = (SocketGuildUser)message.Author;

                Server server = GetBot().GetServerFromSocketGuild(guild);
                ConfigDictionary<string, object> mem = module.serverMemory[server];

                if ((bool)mem.GetValueOrDefault("SetupRunning", false)) {
                    ulong ch = (ulong)mem.GetValue("SetupChannel");
                    ulong usr = (ulong)mem.GetValue("SetupUser");
                    if (ch == channel.Id && usr == user.Id) {
                        if (message.Content.StartsWith("--cancel")) {
                            mem.Put("SetupRunning", false);
                            channel.SendMessageAsync("Cancelled Link/R setup.").GetAwaiter().GetResult();
                        } else if (!message.Content.StartsWith(server.GetPrefix())) {
                            ((Handler)mem.GetValue("CurrentHandler"))(message, channel, user, guild, server, mem);
                        }
                    }
                }

                return Task.CompletedTask;
            };
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "setup-linkr";
        public override string helpsyntax => "";
        public override string description => "configures (or reconfigures) Link/R in the current server";
        public override string permissionnode => "commands.admin.setup.linkr";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;

        private abstract class LinkerSetupElement {
            public abstract string ID {get;}
            public abstract string MessageString {get;}
            public abstract LinkerSetupElement CreateInstance();

            private List<LinkerSetupElement> setup;
            protected LinkerSetupElement GetElement(string ID) {
                foreach (LinkerSetupElement ele in setup) {
                    if (ele.ID == ID)
                        return ele;
                }
                return null;
            }

            public void Prepare(List<LinkerSetupElement> setup) {
                this.setup = setup;
            }

            public abstract bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index);
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }

        private readonly List<LinkerSetupElement> defaultSetup = new List<LinkerSetupElement>(new LinkerSetupElement[] {
            new SetupElementChannel(),
            new SetupElementConfirm(),
            new SetupElementMessage(),
            new SetupElementConfirm(),
            new SetupElementVerChMessage(),
            new SetupElementConfirm(),
            new SetupElementRole(),
            new SetupElementConfirm(),
            new SetupElementNicknameOptions(),
            new SetupElementPostVerificationChannel(),
            new SetupElementConfirm(),
            new SetupElementPostVerMessage(),
            new SetupElementConfirm(),
            new SetupElementFinish()
        });

        private class SetupElementRole : LinkerSetupElement
        {
            public override string ID => "ROLE";

            public override string MessageString => "**Link/R Setup: Step 4:**\n\nPlease select a role to assign to verified members.\nSelect one by mentioning it or chatting the name in this channel.\n\n*Enter verified member role below...*";

            public ulong RoleId = 0;

            public override LinkerSetupElement CreateInstance()
            {
                return new SetupElementRole();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                ulong roleID = 0;
                if (Regex.Match(msg.Content, "^\\<\\@&[0-9]+\\>$").Success) {
                    ulong id = ulong.Parse(msg.Content.Substring(3).Remove(msg.Content.Length - 4));
                    roleID = id;
                } else {
                    foreach (SocketRole r in guild.Roles) {
                        if (r.Name == msg.Content) {
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
                            channel.SendMessageAsync("Selected role '<@&" + r.Id + ">' as verified member role.").GetAwaiter().GetResult();
                            break;
                        }
                    }
                }

                if (!found) {
                    channel.SendMessageAsync("Unable to find a role with that name, please check your message and try again.\n\nPlease select a role to assign to verified members.\nSelect one by mentioning it or chatting the name in this channel.").GetAwaiter().GetResult();
                }

                RoleId = roleID;
                return found;
            }
        }

        private class SetupElementMessage : LinkerSetupElement
        {
            public override string ID => "MESSAGE";

            public string Template = "";

            public override string MessageString => "**Link/R Setup: Step 2:**\n\n"
                                                        + "Please create a verification message template, " 
                                                        + "messages that are created with this template are posted in DM to start the verification process.\n" 
                                                        + "\n" 
                                                        + "Possible value replacements are:\n" 
                                                        + " - `%mention%` - replaced with the user mention\n" 
                                                        + " - `%verificationcode%` - the code that is used by the user to verify their account.\n"
                                                        + " - `%url%` - full Link/R website for verification (tied into the Link/R website associated with the bot)\n" 
                                                        + " - `%urlsuffix%` - the verification part of a Link/R website (example usage: `example.com/linkr/%url%`)\n" 
                                                        + "\n" 
                                                        + "**IMPORTANT**: Any codes generated by Link/R are only valid for 15 minutes!\n" 
                                                        + "\n" 
                                                        + "*Please enter a template below...*";

            public override LinkerSetupElement CreateInstance() => new SetupElementMessage();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                Template = msg.Content;
                if (!Template.Contains("%verificationcode%") && !Template.Contains("%url%") && !Template.Contains("%urlsuffix%")) {
                    channel.SendMessageAsync("***SEVERE WARNING:*** the template does not contain `%verificationcode%`, `%urlsuffix%` or `%url%`, the code will not be showed to the user!").GetAwaiter().GetResult();
                }
                if (!Template.Contains("%mention%")) {
                    channel.SendMessageAsync("**Warning:** `%mention%` is not present in the template, the user will not be pinged by the bot!").GetAwaiter().GetResult();
                }
                channel.SendMessageAsync("Here follows a preview of the message:").GetAwaiter().GetResult();
                string code = Guid.NewGuid().ToString();
                channel.SendMessageAsync(Template.Replace("%verificationcode%", code).Replace("%mention%", "<@" + user.Id + ">").Replace("%url%", Bot.GetBot().GetModule("Link_r").GetConfig().GetValueOrDefault("website", "https://aerialworks.ddns.net/linkr").ToString() + "/%urlsuffix%")
                                        .Replace("%urlsuffix%", "verify?code="+code+"&user=" + user.Id + "&guild=" + guild.Id + "&subsystemaddress=" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')+ (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')
                                            + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')+ (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')
                                            + "&intenttoken=" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')
                                            + "&bot=" + Bot.GetBot().client.CurrentUser.Id)).GetAwaiter().GetResult();
                return true;
            }
        }

        private class SetupElementVerChMessage : LinkerSetupElement
        {
            public override string ID => "VERMESSAGE";

            public string Template = "";

            public override string MessageString => "**Link/R Setup: Step 3:**\n\n"
                                                        + "Please create a verification message template, " 
                                                        + "messages created with this template are posted in the verification channel.\n" 
                                                        + "\n\n" 
                                                        + "Possible value replacements are:\n" 
                                                        + " - `%mention%` - replaced with the user mention\n"
                                                        + "\n\n"
                                                        + "**NOTE:** the text '**React with :thumbsup: to begin the verification process...**'" 
                                                        + " is always appended to verification messages!\n"
                                                        + "\n" 
                                                        + "*Please enter a template below...*";

            public override LinkerSetupElement CreateInstance() => new SetupElementVerChMessage();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                Template = msg.Content;
                if (!Template.Contains("%mention%")) {
                    channel.SendMessageAsync("**Warning:** `%mention%` is not present in the template, the user will not be pinged by the bot!").GetAwaiter().GetResult();
                }
                channel.SendMessageAsync("Here follows a preview of the message:").GetAwaiter().GetResult();
                RestUserMessage msg2 = channel.SendMessageAsync(Template.Replace("%mention%", "<@" + user.Id + ">") + "\n\n**React with :thumbsup: to begin the verification process...**").GetAwaiter().GetResult();
                msg2.AddReactionAsync(new Emoji("\uD83D\uDC4D")).GetAwaiter().GetResult();
                return true;
            }
        }

        private class SetupElementChannel : LinkerSetupElement
        {
            public override string ID => "CHANNEL";

            public ulong SelectedChannel = 0;

            public override string MessageString => "**Link/R Setup: Step 1:**\n\nPlease select a verification channel in which to send user welcome messages.\nTo select a channel, simply mention it in this channel.\n\n*Note: any further messages sent by you in this channel will be interpreted as answers,\ntype '--cancel' to cancel the setup.*";

            public override LinkerSetupElement CreateInstance() => new SetupElementChannel();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                ulong channelID = 0;
                if (Regex.Match(msg.Content, "^\\<\\#[0-9]+\\>$").Success) {
                    ulong id = ulong.Parse(msg.Content.Substring(2).Remove(msg.Content.Length - 3));
                    channelID = id;
                } else {
                    foreach (SocketTextChannel ch in guild.TextChannels) {
                        if (ch.Name == msg.Content) {
                            channelID = ch.Id;
                            break;
                        }
                    }
                }

                bool found = false;
                if (channelID != 0) {
                    foreach (SocketTextChannel ch in guild.TextChannels) {
                        if (ch.Id == channelID) {
                            found = true;
                            channel.SendMessageAsync("Selected channel '<#" + ch.Id + ">' as verification channel.").GetAwaiter().GetResult();
                            break;
                        }
                    }
                }

                if (!found) {
                    channel.SendMessageAsync("Unable to find a channel with that name, or don't have acces to it, please check your message and try again.\n\nPlease select a channel in which to post user welcome messages.\nTo select a channel, simply mention it in this channel.").GetAwaiter().GetResult();
                }

                SelectedChannel = channelID;
                return found;
            }
        }

        private class SetupElementPostVerificationChannel : LinkerSetupElement
        {
            public override string ID => "POSTVERCHANNEL";

            public ulong SelectedChannel = 0;

            public override string MessageString => "**Link/R Setup: Step 6:**\n\nPlease select a post-verification channel, in this channel, post-verification welcome messages will be send.\nTo select a channel, simply mention it in this channel.";

            public override LinkerSetupElement CreateInstance() => new SetupElementPostVerificationChannel();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                ulong channelID = 0;
                if (Regex.Match(msg.Content, "^\\<\\#[0-9]+\\>$").Success) {
                    ulong id = ulong.Parse(msg.Content.Substring(2).Remove(msg.Content.Length - 3));
                    channelID = id;
                } else {
                    foreach (SocketTextChannel ch in guild.TextChannels) {
                        if (ch.Name == msg.Content) {
                            channelID = ch.Id;
                            break;
                        }
                    }
                }

                bool found = false;
                if (channelID != 0) {
                    foreach (SocketTextChannel ch in guild.TextChannels) {
                        if (ch.Id == channelID) {
                            found = true;
                            channel.SendMessageAsync("Selected channel '<#" + ch.Id + ">' as post-verification channel.").GetAwaiter().GetResult();
                            break;
                        }
                    }
                }

                if (!found) {
                    channel.SendMessageAsync("Unable to find a channel with that name, or don't have acces to it, please check your message and try again.\n\nPlease select a post-verification channel.\nTo select a channel, simply mention it in this channel.").GetAwaiter().GetResult();
                }

                SelectedChannel = channelID;
                return found;
            }
        }

        private class SetupElementPostVerMessage : LinkerSetupElement
        {
            public override string ID => "POSTVERMESSAGE";

            public string Template = "";

            public override string MessageString => "**Link/R Setup: Step 7:**\n\nPlease create a post-verification message template, messages that are created with this template are send to the post-verification channel when a user completes the verification process.\n\nPossible value replacements are:\n - `%mention%` - replaced with the user mention\n\n*Please enter a template below...*";

            public override LinkerSetupElement CreateInstance() => new SetupElementPostVerMessage();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                Template = msg.Content;
                if (!Template.Contains("%mention%")) {
                    channel.SendMessageAsync("**Warning:** `%mention%` is not present in the template, the user will not be pinged by the bot!").GetAwaiter().GetResult();
                }
                channel.SendMessageAsync("Here follows a preview of the message:").GetAwaiter().GetResult();
                channel.SendMessageAsync(Template.Replace("%mention%", "<@" + user.Id + ">")).GetAwaiter().GetResult();
                return true;
            }
        }

        private class SetupElementConfirm : LinkerSetupElement
        {
            public override string ID => "CONFIRM";

            public override string MessageString => "Do you want to continue? [Y/n]";

            public override LinkerSetupElement CreateInstance() => new SetupElementConfirm();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {   
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    index -= 2;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want to continue? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementNicknameOptions : LinkerSetupElement
        {
            public override string ID => "NICKNAME";

            public override string MessageString => "**Link/R Setup: Step 5:**\n\nDo you want to override member nicknames?\nSelect none if you want to use Discord nicknames, select username to override member nicknames to use Roblox usernames, select displayname to override nicknames to use Roblox displaynames.\n\n*Select a option... [None/username/displayname]*";
            public string SelectedOption = "";

            public override LinkerSetupElement CreateInstance() => new SetupElementNicknameOptions();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {   
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "none")
                {
                    SelectedOption = "none";
                    return true;
                }
                if (msg.Content.ToLower() == "u" || msg.Content.ToLower() == "username")
                {
                    SelectedOption = "username";
                    return true;
                }
                if (msg.Content.ToLower() == "d" || msg.Content.ToLower() == "displayname")
                {
                    SelectedOption = "displayname";
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting none/username/displayname.\n\nSelect a option... [None/username/displayname]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementFinish : LinkerSetupElement
        {
            public override string ID => "END";

            public override string MessageString => "**Link/R Setup: Confirm Settings**\n\n" +
                                                        "Link/R setup has been completed, please confirm your settings:\n\n" + 
                                                        " - **Selected verification channel:** <#" + ((SetupElementChannel)GetElement("CHANNEL")).SelectedChannel + ">\n" + 
                                                        " - **Selected verified member role:** <@&" + ((SetupElementRole)GetElement("ROLE")).RoleId + ">\n" + 
                                                        " - **Selected nickname override method:** *" + ((SetupElementNicknameOptions)GetElement("NICKNAME")).SelectedOption + "*\n" +
                                                        " - **Selected post-verification channel:** <#" + ((SetupElementPostVerificationChannel)GetElement("POSTVERCHANNEL")).SelectedChannel + ">\n" + 
                                                        " - **DM template message:**\n```\n" + ((SetupElementMessage)GetElement("MESSAGE")).Template.Replace("```", "'''") + "\n```\n\n" +
                                                        " - **Verification template message:**\n```\n" + ((SetupElementVerChMessage)GetElement("VERMESSAGE")).Template.Replace("```", "'''") + "\n```\n\n" +
                                                        " - **Post-verification template message:**\n```\n" + ((SetupElementPostVerMessage)GetElement("POSTVERMESSAGE")).Template.Replace("```", "'''") + "\n```\n\n" +
                                                        "***Save these settings? [Y/n]***";

            public override LinkerSetupElement CreateInstance() => new SetupElementFinish();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    channel.SendMessageAsync("***Saving configuration...***").GetAwaiter().GetResult();
                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.message.template", ((SetupElementMessage)GetElement("MESSAGE")).Template);
                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.memberrole", ((SetupElementRole)GetElement("ROLE")).RoleId);
                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.channel", ((SetupElementChannel)GetElement("CHANNEL")).SelectedChannel);
                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.channel.message.template", ((SetupElementVerChMessage)GetElement("VERMESSAGE")).Template);

                    if (((SetupElementNicknameOptions)GetElement("NICKNAME")).SelectedOption == "none") {
                        server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.nicknames.overridenickname", false);
                        server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.nicknames.usedisplayname", false);
                    } else if (((SetupElementNicknameOptions)GetElement("NICKNAME")).SelectedOption == "username") {
                        server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.nicknames.overridenickname", true);
                        server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.nicknames.usedisplayname", false);
                    } else if (((SetupElementNicknameOptions)GetElement("NICKNAME")).SelectedOption == "displayname") {
                        server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.nicknames.overridenickname", true);
                        server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.nicknames.usedisplayname", true);
                    }
                    
                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.postver.message.template", ((SetupElementPostVerMessage)GetElement("POSTVERMESSAGE")).Template);
                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("verification.postver.channel", ((SetupElementPostVerificationChannel)GetElement("POSTVERCHANNEL")).SelectedChannel);

                    server.GetModuleConfig(Bot.GetBot().GetModule("Link_r")).Set("SetupCompleted", true);
                    server.SaveAll();
                    channel.SendMessageAsync("***Configuration has been saved.***").GetAwaiter().GetResult();

                    foreach (Server srv in Bot.GetBot().servers) {
                        var en = Bot.GetBot().client.GetGuild(srv.id).GetUsersAsync().GetAsyncEnumerator();
                        while (true) {
                            if (en.Current != null) {
                                foreach (IGuildUser usr in en.Current) {
                                    if (usr is SocketGuildUser) {
                                        ((Module.GuildUserEvent)mem["MemberSyncHandler"])((SocketGuildUser)usr);
                                    }
                                }
                            }
                            if (!en.MoveNextAsync().GetAwaiter().GetResult())
                                break;
                        }
                    }
                    
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    index = -1;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nSave these settings? [Y/n]").GetAwaiter().GetResult();
                return true;
            }
        }

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            ConfigDictionary<string, object> mem = module.serverMemory[server];

            if ((bool)mem.GetValueOrDefault("SetupRunning", false)) {
                await channel.SendMessageAsync("Link/R setup has already been started by <@" + mem.GetValueOrDefault("SetupUser", -1) + ">, run " + GetBot().prefix + "cancel-linkr-setup to cancel.");
            } else {
                mem.Put("SetupChannel", channel.Id);
                mem.Put("SetupUser", user.Id);
                mem.Put("SetupRunning", true);

                List<LinkerSetupElement> setup = new List<LinkerSetupElement>(defaultSetup.Select(t => t.CreateInstance()));

                int index = 0;
                void startHandler(LinkerSetupElement ele) {
                    ele.Prepare(setup);
                    channel.SendMessageAsync(ele.MessageString).GetAwaiter().GetResult();
                    chatHandler(ele);
                }

                void chatHandler(LinkerSetupElement ele) {
                    mem.Put("CurrentHandler", new Handler((SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem) => {
                        if (ele.Process(msg, channel, user, guild, server, mem, ref index))
                            if (index + 1 < setup.Count) {
                                index++;
                                startHandler(setup[index]);
                            } else {
                                mem.Put("SetupRunning", false);
                                return;
                            }
                        else
                            chatHandler(ele);
                    }));
                }

                startHandler(setup[index]);
            }
        }
        
    }
}
