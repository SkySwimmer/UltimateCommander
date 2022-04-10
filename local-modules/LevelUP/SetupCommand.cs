using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMDR;
using Discord.WebSocket;

namespace levelup {
    public class SetupCommand : SystemCommand
    {
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
                            channel.SendMessageAsync("Cancelled LevelUP setup.").GetAwaiter().GetResult();
                        } else if (!message.Content.StartsWith(server.GetPrefix())) {
                            ((Handler)mem.GetValue("CurrentHandler"))(message, channel, user, guild, server, mem);
                        }
                    }
                }

                return Task.CompletedTask;
            };
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };

        public override string commandid => "levelup-setup";
        public override string helpsyntax => "";
        public override string description => "configures (or reconfigures) the LevelUP bot in this server";
        public override string permissionnode => "commands.admin.setup.levelup";

        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;

        private abstract class SetupElement {
            public abstract string ID {get;}
            public abstract string MessageString {get;}
            public abstract SetupElement CreateInstance();

            private List<SetupElement> setup;
            protected SetupElement GetElement(string ID) {
                foreach (SetupElement ele in setup) {
                    if (ele.ID == ID)
                        return ele;
                }
                return null;
            }

            public void Prepare(List<SetupElement> setup) {
                this.setup = setup;
            }

            public abstract bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index);
        }

        private readonly List<SetupElement> defaultSetup = new List<SetupElement>(new SetupElement[] {
            // Level advancement
            new SetupElementSendAdvancementMessages(),
            new SetupElementUseDefaultChannel(),
            new SetupElementAdvancementChannel(),
            new SetupElementConfirm(2),
            new SetupElementAdvancementMessage(),
            new SetupElementConfirm(),

            // Role advancement
            new SetupElementSendRoleAdvancementMessages(),
            new SetupElementUseDefaultChannelForRoles(),
            new SetupElementRoleAdvancementChannel(),
            new SetupElementConfirm(2),
            new SetupElementRoleAdvancementMessage(),
            new SetupElementConfirm(),

            // Levels
            new SetupElementMaxLevel(),
            new SetupElementConfirm(),

            // Experience
            new SetupElementBaseLevelUpXP(),
            new SetupElementConfirm(),
            new SetupElementXPMultiplier(),
            new SetupElementConfirm(),
            new SetupElementXPCharacterLimit(),
            new SetupElementConfirm(),
            new SetupElementConfirmXPSettings(),

            // Roles
            new SetupElementConfigureRoles(),
            new SetupElementSelectRoles(),
            new SetupElementConfirm(),

            // Finish configuration and save
            new SetupElementFinish()
        });

        private class SetupElementFinish : SetupElement
        {
            public override string ID => "END";

            public override string MessageString {
                get {
                    string msg = "**LevelUP Setup: Confirm Settings**\n";
                    msg += "\n";
                    msg += "LevelUP setup has been completed, please confirm your settings:\n";

                    SetupElementSendAdvancementMessages sAM = (SetupElementSendAdvancementMessages)GetElement("SENDADVANCEMENTMESSAGES");
                    msg += "**Send messages when users advance:** *" + sAM.Active + "*\n";
                    if (sAM.Active) {
                        SetupElementUseDefaultChannel uDC = (SetupElementUseDefaultChannel)GetElement("ADVANCEMENTUSEDEFAULTCHANNEL");
                        SetupElementAdvancementChannel sC = (SetupElementAdvancementChannel)GetElement("ADVANCEMENTCHANNEL");
                        if (uDC.Active) {
                            msg += "**Advancement message channel:** *User Current Channel*\n";
                        } else {
                            msg += "**Advancement message channel:** <#" + sC.SelectedChannel + ">\n";
                        }
                    }

                    SetupElementSendRoleAdvancementMessages srAM = (SetupElementSendRoleAdvancementMessages)GetElement("SENDROLEADVANCEMENTMESSAGES");
                    msg += "**Send messages when users advance in roles:** *" + srAM.Active + "*\n";
                    if (srAM.Active) {
                        SetupElementUseDefaultChannelForRoles uDC = (SetupElementUseDefaultChannelForRoles)GetElement("ROLEADVANCEMENTUSEDEFAULTCHANNEL");
                        SetupElementRoleAdvancementChannel sC = (SetupElementRoleAdvancementChannel)GetElement("ROLEADVANCEMENTCHANNEL");
                        if (uDC.Active) {
                            msg += "**Role advancement message channel:** *User Current Channel*\n";
                        } else {
                            msg += "**Role advancement message channel:** <#" + sC.SelectedChannel + ">\n";
                        }
                    }

                    int multiplier = ((SetupElementXPMultiplier)GetElement("XPMULTIPLIER")).XPMultiplier;
                    int maxXPIncrease = ((SetupElementXPCharacterLimit)GetElement("XPCHARLIMIT")).XPCharLimit;
                    int defaultLevelBaseXP = ((SetupElementBaseLevelUpXP)GetElement("BASELEVELUPXP")).LevelUpXP;

                    msg += "\n";
                    msg += "**Max Level:** *" + ((SetupElementMaxLevel)GetElement("MAXLEVEL")).MaxLevel + "*\n";
                    msg += "**XP Multiplier:** *" + multiplier + "*\n";
                    msg += "**XP Character Limit:** *" + maxXPIncrease + "*\n";
                    msg += "**Base Advancement XP:** *" + defaultLevelBaseXP + "*\n";

                    string roleMsg = "";
                    foreach (int level in ((SetupElementSelectRoles)GetElement("ADVANCEMENTROLES")).SelectedRoles.Keys) {
                        roleMsg += " - Level " + level + ": <@&" + ((SetupElementSelectRoles)GetElement("ADVANCEMENTROLES")).SelectedRoles[level] + ">\n";
                    }

                    if (sAM.Active)
                        msg += "\n**Advancement message template:**\n```\n" + ((SetupElementAdvancementMessage)GetElement("ADVANCEMENTMESSAGE")).Template.Replace("```", "'''") + "\n```";
                    if (srAM.Active)
                        msg += "\n**Role advancement message template:**\n```\n" + ((SetupElementRoleAdvancementMessage)GetElement("ROLEADVANCEMENTMESSAGE")).Template.Replace("```", "'''") + "\n```";
                    if (((SetupElementConfigureRoles)GetElement("CONFIGUREROLES")).Active) {
                        msg += "\n";
                        msg += "**Level Roles:**\n" + roleMsg + "\n";
                    }

                    msg += "\n";
                    msg += "***Save these settings? [Y/n]***";

                    return msg;
                }
            }

            public override SetupElement CreateInstance() => new SetupElementFinish();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    channel.SendMessageAsync("***Saving configuration...***").GetAwaiter().GetResult();

                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("xp.increase.max", ((SetupElementXPCharacterLimit)GetElement("XPCHARLIMIT")).XPCharLimit);
                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("xp.increase.multiplier.max", ((SetupElementXPMultiplier)GetElement("XPMULTIPLIER")).XPMultiplier);
                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("xp.levelup.base", ((SetupElementBaseLevelUpXP)GetElement("BASELEVELUPXP")).LevelUpXP);
                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("xp.maxlevel", ((SetupElementMaxLevel)GetElement("MAXLEVEL")).MaxLevel);

                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.sendonadvancement", ((SetupElementSendAdvancementMessages)GetElement("SENDADVANCEMENTMESSAGES")).Active);
                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.advancement.usecurrentchannel", ((SetupElementUseDefaultChannel)GetElement("ADVANCEMENTUSEDEFAULTCHANNEL")).Active);
                    if (!((SetupElementUseDefaultChannel)GetElement("ADVANCEMENTUSEDEFAULTCHANNEL")).Active) {
                        server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.advancement.channel", ((SetupElementAdvancementChannel)GetElement("ADVANCEMENTCHANNEL")).SelectedChannel);
                    }
                    if (((SetupElementSendAdvancementMessages)GetElement("SENDADVANCEMENTMESSAGES")).Active) {
                        server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.advancement.template", ((SetupElementAdvancementMessage)GetElement("ADVANCEMENTMESSAGE")).Template);
                    }

                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.sendonroleadvancement", ((SetupElementSendRoleAdvancementMessages)GetElement("SENDROLEADVANCEMENTMESSAGES")).Active);
                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.roleadvancement.usecurrentchannel", ((SetupElementUseDefaultChannelForRoles)GetElement("ROLEADVANCEMENTUSEDEFAULTCHANNEL")).Active);
                    if (!((SetupElementUseDefaultChannelForRoles)GetElement("ROLEADVANCEMENTUSEDEFAULTCHANNEL")).Active) {
                        server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.roleadvancement.channel", ((SetupElementRoleAdvancementChannel)GetElement("ROLEADVANCEMENTCHANNEL")).SelectedChannel);
                    }
                    if (((SetupElementSendRoleAdvancementMessages)GetElement("SENDROLEADVANCEMENTMESSAGES")).Active) {
                        server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("messages.roleadvancement.template", ((SetupElementRoleAdvancementMessage)GetElement("ROLEADVANCEMENTMESSAGE")).Template);
                    }

                    ConfigDictionary<int, ulong> levelRoles = new ConfigDictionary<int, ulong>();
                    if (((SetupElementConfigureRoles)GetElement("CONFIGUREROLES")).Active) {
                        levelRoles = ((SetupElementSelectRoles)GetElement("ADVANCEMENTROLES")).SelectedRoles;
                    }
                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("levelroles", Serializer.Serialize(levelRoles));

                    server.GetModuleConfig(Bot.GetBot().GetModule("LevelUP")).Set("SetupCompleted", true);
                    server.SaveAll();
                    channel.SendMessageAsync("***Configuration has been saved.***\n***Please note that you need to prune all member levels before these settings can fully apply***").GetAwaiter().GetResult();

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

        private class SetupElementSelectRoles : SetupElement
        {
            public override string ID => "ADVANCEMENTROLES";
            public ConfigDictionary<int, ulong> SelectedRoles = new ConfigDictionary<int, ulong>();

            public override string MessageString => "**LevelUP Setup: Step 9:**\n"
                    + "\n"
                    + "\n"
                    + "**To configure level roles, create a list with the following template:**\n"
                    + "```\n"
                    + "<level>: @<role>\n"
                    + "```\n"
                    + "**Example:**\n"
                    + "```\n"
                    + "2: @Trusted Member\n"
                    + "6: @Trusted Member (tier 2)\n"
                    + "```\n"
                    + "\n"
                    + "**Note:** you cannot assign roles to level one.\n"
                    + "*Enter role configuration below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementSelectRoles();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                int c = 1;
                SelectedRoles.Clear();
                foreach (string ln in msg.Content.Replace("\r", "").Split("\n")) {
                    string line = ln.Trim();
                    if (!Regex.Match(line, "^[0-9]+\\: \\<\\@&[0-9]+\\>$").Success) {
                        channel.SendMessageAsync("**Error:** incorrect syntax used in role configuration, at line " + c + "\n\n**To configure level roles, create a list with the following template:**\n"
                                + "```\n"
                                + "<level>: @<role>\n"
                                + "```\n"
                                + "**Example:**\n"
                                + "```\n"
                                + "2: @Trusted Member\n"
                                + "6: @Trusted Member (tier 2)\n"
                                + "```\n"
                                + "\n"
                                + "**Note:** you cannot assign roles to level one.\n"
                                + "*Enter role configuration below...*").GetAwaiter().GetResult();
                        return false;
                    } else {
                        try {
                            int level = int.Parse(line.Remove(line.IndexOf(":")));
                            ulong role = ulong.Parse(line.Substring(line.IndexOf(": ") + 5).Remove(line.Substring(line.IndexOf(": ") + 5).LastIndexOf(">")));

                            SocketRole r = guild.GetRole(role);
                            if (level < 2 || level > ((SetupElementMaxLevel)GetElement("MAXLEVEL")).MaxLevel) {
                                channel.SendMessageAsync("**Error:** invalid level used in role configuration, at line " + c + "\n\n**To configure level roles, create a list with the following template:**\n"
                                        + "```\n"
                                        + "<level>: @<role>\n"
                                        + "```\n"
                                        + "**Example:**\n"
                                        + "```\n"
                                        + "2: @Trusted Member\n"
                                        + "6: @Trusted Member (tier 2)\n"
                                        + "```\n"
                                        + "\n"
                                        + "**Note:** you cannot assign roles to level one.\n"
                                        + "*Enter role configuration below...*").GetAwaiter().GetResult();
                                return false;
                            } else {
                                if (r == null) {
                                    channel.SendMessageAsync("**Error:** invalid role mention used in role configuration, at line " + c + "\n\n**To configure level roles, create a list with the following template:**\n"
                                            + "```\n"
                                            + "<level>: @<role>\n"
                                            + "```\n"
                                            + "**Example:**\n"
                                            + "```\n"
                                            + "2: @Trusted Member\n"
                                            + "6: @Trusted Member (tier 2)\n"
                                            + "```\n"
                                            + "\n"
                                            + "**Note:** you cannot assign roles to level one.\n"
                                            + "*Enter role configuration below...*").GetAwaiter().GetResult();
                                    return false;
                                } else {
                                    SelectedRoles[level] = role;
                                }
                            }
                        } catch {
                            channel.SendMessageAsync("**Error:** incorrect syntax used in role configuration, at line " + c + "\n\n**To configure level roles, create a list with the following template:**\n"
                                    + "```\n"
                                    + "<level>: @<role>\n"
                                    + "```\n"
                                    + "**Example:**\n"
                                    + "```\n"
                                    + "2: @Trusted Member\n"
                                    + "6: @Trusted Member (tier 2)\n"
                                    + "```\n"
                                    + "\n"
                                    + "**Note:** you cannot assign roles to level one.\n"
                                    + "*Enter role configuration below...*").GetAwaiter().GetResult();
                            return false;
                        }
                    }
                    c++;
                }

                string response = "Role configuration:\n";
                foreach (int level in SelectedRoles.Keys) {
                    response += " - Level " + level + ": <@&" + SelectedRoles[level] + ">\n";
                }
                channel.SendMessageAsync(response);
                
                return true;
            }
        }

        private class SetupElementConfigureRoles : SetupElement
        {
            public override string ID => "CONFIGUREROLES";
            public bool Active = false;

            public override string MessageString => "**LevelUP Setup: Step 8:**\n"
                    + "\n"
                    + "Do you want to configure roles to grant when users reach specific levels? [Y/n]";

            public override SetupElement CreateInstance()
            {
                return new SetupElementConfigureRoles();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    Active = true;
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    Active = false;
                    index += 2;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want to configure roles to grant when users reach specific levels? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementBaseLevelUpXP : SetupElement {
            public override string ID => "BASELEVELUPXP";
            public int LevelUpXP = 0;

            public override string MessageString => "**LevelUP Setup: Step 4:**\n"
                    + "\n"
                    + "Please select the experience amount that is needed for the fist advancement.\n" 
                    + "The default advancement experience amount is 1400.\n"
                    + "\n" 
                    + "*Please enter the experience needed to advance to level 2 below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementBaseLevelUpXP();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                try {
                    LevelUpXP = int.Parse(msg.Content);
                    if (LevelUpXP < 0 || LevelUpXP > int.MaxValue / 10000) {
                        channel.SendMessageAsync("**Error:** expecting a non-negative number (below " + int.MaxValue / 10000 + ").\n\nPlease select the experience amount that is needed for the fist advancement.\n" 
                                + "The default advancement experience amount is 1400.\n"
                                + "\n" 
                                + "*Please enter the experience needed to advance to level 2 below...*").GetAwaiter().GetResult();
                        return false;
                    }

                channel.SendMessageAsync("Selected base advancement XP amount: " + LevelUpXP).GetAwaiter().GetResult();
                    return true;
                } catch {
                    channel.SendMessageAsync("**Error:** expecting a number.\n\nPlease select the experience amount that is needed for the fist advancement.\n" 
                            + "The default advancement experience amount is 1400.\n"
                            + "\n" 
                            + "*Please enter the experience needed to advance to level 2 below...*").GetAwaiter().GetResult();
                    return false;
                }
            }

        }

        private class SetupElementConfirmXPSettings : SetupElement
        {
            public override string ID => "CONFIRM";

            public override string MessageString {
                get {
                    string msg = "**LevelUP Setup: Step 7:**\n";
                    msg += "\n";
                    msg += "\n";
                    msg += "Please confirm the experience settings:\n";

                    int multiplier = ((SetupElementXPMultiplier)GetElement("XPMULTIPLIER")).XPMultiplier;
                    int maxXPIncrease = ((SetupElementXPCharacterLimit)GetElement("XPCHARLIMIT")).XPCharLimit;
                    int defaultLevelBaseXP = ((SetupElementBaseLevelUpXP)GetElement("BASELEVELUPXP")).LevelUpXP;

                    msg += "**XP Multiplier:** *" + multiplier + "*\n";
                    msg += "**XP Character Limit:** *" + maxXPIncrease + "*\n";
                    msg += "**XP Needed for the first Advancement:** *" + defaultLevelBaseXP + "*\n";
                    msg += "\n";
                    msg += "\n";
                    msg += "Simulated advancements:\n";

                    int msgs = 0;
                    Module.UserLevel level = new Module.UserLevel() {
                        LevelUpXP = defaultLevelBaseXP,
                        Level = 1
                    };
                    for (int i = 2; i <= 15; i++) {
                        while (level.Level < i) {
                            int xp = rnd.Next(1, multiplier) * maxXPIncrease;
                            level.TotalXP += xp;
                            level.CurrentXP += xp;
                            msgs += 1;
                            while (level.CurrentXP > level.LevelUpXP) {
                                level.Level++;
                                level.CurrentXP -= level.LevelUpXP;
                                level.LevelUpXP = level.LevelUpXP + (level.LevelUpXP / 4);
                            }
                        }
                        msg += "**Level " + (i - 1) + " to level " + i + ":** *~" + msgs + " messages of " + maxXPIncrease + " characters." + "*\n";
                        msgs = 0;
                    }
                    msg += "To simulate a specific level with your current settings, enter `simulate <level>`, example: `simulate 10`\n";
                    msg += "\n";
                    msg += "\n";
                    msg += "Do you want to continue? [Y/n]";
                    return msg;
                }
            }

            public override SetupElement CreateInstance() => new SetupElementConfirmXPSettings();

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower().StartsWith("simulate ")) {
                    string args = msg.Content.Substring("simulate ".Length);
                    try {
                        int lv = int.Parse(args);
                        int msgs = 0;
                        int multiplier = ((SetupElementXPMultiplier)GetElement("XPMULTIPLIER")).XPMultiplier;
                        int maxXPIncrease = ((SetupElementXPCharacterLimit)GetElement("XPCHARLIMIT")).XPCharLimit;
                        int defaultLevelBaseXP = ((SetupElementBaseLevelUpXP)GetElement("BASELEVELUPXP")).LevelUpXP;
                        Module.UserLevel level = new Module.UserLevel() {
                            LevelUpXP = defaultLevelBaseXP,
                            Level = 1
                        };
                        while (level.Level < lv) {
                            int xp = rnd.Next(1, multiplier) * maxXPIncrease;
                            level.TotalXP += xp;
                            level.CurrentXP += xp;
                            msgs += 1;
                            while (level.CurrentXP > level.LevelUpXP) {
                                level.Level++;
                                level.CurrentXP -= level.LevelUpXP;
                                level.LevelUpXP = level.LevelUpXP + (level.LevelUpXP / 4);
                            }
                        }
                        channel.SendMessageAsync("**Level 1 to level " + lv + ":** *~" + msgs + " messages of " + maxXPIncrease + " characters.*").GetAwaiter().GetResult();
                    } catch {
                        channel.SendMessageAsync("**Error:** `level` is not a valid number.").GetAwaiter().GetResult();
                    }
                    channel.SendMessageAsync("Do you want to continue? [Y/n]").GetAwaiter().GetResult();
                    return false;
                }
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    index -= 7;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want to continue? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementXPMultiplier : SetupElement {
            public override string ID => "XPMULTIPLIER";
            public int XPMultiplier = 0;

            public override string MessageString => "**LevelUP Setup: Step 5:**\n"
                    + "\n"
                    + "Please select a XP multiplier, to select a XP multiplier, simply enter a number below...\n" 
                    + "The default XP multiplier is 10.\n"
                    + "\n" 
                    + "*Please enter the XP multiplier below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementXPMultiplier();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                try {
                    XPMultiplier = int.Parse(msg.Content);
                    if (XPMultiplier < 1 || XPMultiplier > int.MaxValue / 10000) {
                        channel.SendMessageAsync("**Error:** expecting a positive non-zero number (below " + int.MaxValue / 10000 + ").\n\nPlease select a XP multiplier, to select a XP multiplier, simply enter a number below...\n" 
                                + "The default XP multiplier is 10.\n"
                                + "\n" 
                                + "*Please enter the XP multiplier below...*").GetAwaiter().GetResult();
                        return false;
                    }

                    channel.SendMessageAsync("Selected XP multiplier: " + XPMultiplier).GetAwaiter().GetResult();
                    return true;
                } catch {
                    channel.SendMessageAsync("**Error:** expecting a number.\n\nPlease select a XP multiplier, to select a XP multiplier, simply enter a number below...\n" 
                                + "The default XP multiplier is 10.\n"
                                + "\n" 
                                + "*Please enter the XP multiplier below...*").GetAwaiter().GetResult();
                    return false;
                }
            }

        }

        private class SetupElementXPCharacterLimit : SetupElement {
            public override string ID => "XPCHARLIMIT";
            public int XPCharLimit = 0;

            public override string MessageString => "**LevelUP Setup: Step 6:**\n"
                    + "\n"
                    + "Please select a XP character limit, the character limit is used by the XP randomizer.\n" 
                    + "\n" 
                    + "To select a XP character limit, simply enter a number below...\n"
                    + "The default XP character limit is 12.\n"
                    + "\n" 
                    + "*Please enter the XP character limit below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementXPCharacterLimit();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                try {
                    XPCharLimit = int.Parse(msg.Content);
                    if (XPCharLimit < 1 || XPCharLimit > int.MaxValue / 10000) {
                        channel.SendMessageAsync("**Error:** expecting a positive non-zero number (below " + int.MaxValue / 10000 + ").\n\nPlease select a XP character limit, the character limit is used by the XP randomizer.\n" 
                            + "\n" 
                            + "To select a XP character limit, simply enter a number below...\n"
                            + "The default XP character limit is 12.\n"
                            + "\n" 
                            + "*Please enter the XP character limit below...*").GetAwaiter().GetResult();
                        return false;
                    }

                    channel.SendMessageAsync("Selected XP character limit: " + XPCharLimit).GetAwaiter().GetResult();
                    return true;
                } catch {
                    channel.SendMessageAsync("**Error:** expecting a number.\n\nPlease select a XP character limit, the character limit is used by the XP randomizer.\n" 
                            + "\n" 
                            + "To select a XP character limit, simply enter a number below...\n"
                            + "The default XP character limit is 12.\n"
                            + "\n" 
                            + "*Please enter the XP character limit below...*").GetAwaiter().GetResult();
                    return false;
                }
            }

        }

        private class SetupElementMaxLevel : SetupElement {
            public override string ID => "MAXLEVEL";
            public int MaxLevel = 0;

            public override string MessageString => "**LevelUP Setup: Step 3:**\n"
                    + "\n"
                    + "Please select a max level, to select a maximum level, simply enter a number below...\n" 
                    + "The default max level is 1000.\n"
                    + "\n" 
                    + "*Please enter a max level number below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementMaxLevel();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                try {
                    MaxLevel = int.Parse(msg.Content);
                    if (MaxLevel < 0 || MaxLevel > int.MaxValue / 10000) {
                        channel.SendMessageAsync("**Error:** expecting a non-negative number (below " + int.MaxValue / 10000 + ").\n\nPlease select a max level, to select a maximum level, simply enter a number below...\n" 
                                + "The default max level is 1000.\n"
                                + "\n" 
                                + "*Please enter a max level number below...*").GetAwaiter().GetResult();
                        return false;
                    }

                channel.SendMessageAsync("Selected max level: " + MaxLevel).GetAwaiter().GetResult();
                    return true;
                } catch {
                    channel.SendMessageAsync("**Error:** expecting a number.\n\nPlease select a max level, to select a maximum level, simply enter a number below...\n" 
                            + "The default max level is 1000.\n"
                            + "\n" 
                            + "*Please enter a max level number below...*").GetAwaiter().GetResult();
                    return false;
                }
            }

        }

        private class SetupElementSendRoleAdvancementMessages : SetupElement
        {
            public override string ID => "SENDROLEADVANCEMENTMESSAGES";
            public bool Active = false;

            public override string MessageString => "**LevelUP Setup: Step 2:**\n"
                    + "\n"
                    + "Do you want the bot to send a message if a user advances in level role? [Y/n]";

            public override SetupElement CreateInstance()
            {
                return new SetupElementSendRoleAdvancementMessages();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    Active = true;
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    Active = false;
                    index += 5;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want the bot to send a message if a user advances in level role? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementUseDefaultChannelForRoles : SetupElement
        {
            public override string ID => "ROLEADVANCEMENTUSEDEFAULTCHANNEL";
            public bool Active = false;

            public override string MessageString => "**LevelUP Setup: Step 2.1:**\n"
                    + "\n"
                    + "Do you want to post role advancement messages in a user's active channel? [Y/n]\n";

            public override SetupElement CreateInstance()
            {
                return new SetupElementUseDefaultChannelForRoles();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    Active = true;
                    index += 2;
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    Active = false;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want to post role advancement messages in a user's active channel? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementRoleAdvancementChannel : SetupElement
        {
            public override string ID => "ROLEADVANCEMENTCHANNEL";
            public ulong SelectedChannel;

            public override string MessageString => "**LevelUP Setup: Step 2.1.1:**\n"
                    + "\n"
                    + "Please select a channel in which to post user role advancement messages.\n"
                    + "To select a channel, simply mention it in this channel.\n"
                    + "\n"
                    + "*Please mention a channel below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementRoleAdvancementChannel();
            }

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
                            channel.SendMessageAsync("Selected channel '<#" + ch.Id + ">' (in '" + ch.Category.Name + "') as role advancement channel.").GetAwaiter().GetResult();
                            break;
                        }
                    }
                }

                if (!found) {
                    channel.SendMessageAsync("Unable to find a channel with that name, or don't have acces to it, please check your message and try again.\n\nPlease select a channel in which to post user role advancement messages.\nTo select a channel, simply mention it in this channel.").GetAwaiter().GetResult();
                }

                SelectedChannel = channelID;
                return found;
            }
        }

        private class SetupElementRoleAdvancementMessage : SetupElement
        {
            public override string ID => "ROLEADVANCEMENTMESSAGE";
            public string Template;

            public override string MessageString => "**LevelUP Setup: Step 2.2:**\n"
                    + "\n"
                    + "Please create a role advancement message template, messages created with this template are posted in the role advancements channel.\n"
                    + "\n"
                    + "Possible value replacements are:\n" 
                    + " - `%mention%` - replaced with the user mention\n"
                    + " - `%name%` - replaced with the user nickname\n"
                    + " - `%level%` - replaced with the current user level (after advancing)\n"
                    + " - `%role%` - replaced with the role the user received\n"
                    + " - `%currentxp%` - replaced with the remaining XP of the user (after advancing)\n"
                    + " - `%levelupxp%` - replaced with the max XP for advancing to the next level\n"
                    + "\n"
                    + "**Note:** the level advancement message is not posted if the user advances in role.\n"
                    + "\n"
                    + "*Please enter a template below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementRoleAdvancementMessage();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                Template = msg.Content;
                if (!Template.Contains("%mention%")) {
                    channel.SendMessageAsync("**Warning:** `%mention%` is not present in the template, the user will not be pinged by the bot!").GetAwaiter().GetResult();
                }
                if (!Template.Contains("%role%")) {
                    channel.SendMessageAsync("**Warning:** `%role%` is not present in the template, the user wont see their new role!").GetAwaiter().GetResult();
                }
                channel.SendMessageAsync("Here follows a preview of the message:").GetAwaiter().GetResult();
                channel.SendMessageAsync(Template
                        .Replace("%mention%", "<@" + user.Id + ">")
                        .Replace("%name%", (user.Nickname == null || user.Nickname == "" ? user.Username : user.Nickname))
                        .Replace("%level%", "3")
                        .Replace("%role%", "`Example Role`")
                        .Replace("%currentxp%", "6")
                        .Replace("%levelupxp%", "3250")).GetAwaiter().GetResult();
                return true;
            }
        }


        private class SetupElementSendAdvancementMessages : SetupElement
        {
            public override string ID => "SENDADVANCEMENTMESSAGES";
            public bool Active = false;

            public override string MessageString => "**LevelUP Setup: Step 1:**\n"
                    + "\n"
                    + "Do you want the bot to send a message if a user advances in level? [Y/n]\n"
                    + "\n"
                    + "*Note: any further messages sent by you in this channel will be interpreted as answers,\n" 
                    + "type '--cancel' to cancel the setup.*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementSendAdvancementMessages();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    Active = true;
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    Active = false;
                    index += 5;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want the bot to send a message if a user advances in level? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementUseDefaultChannel : SetupElement
        {
            public override string ID => "ADVANCEMENTUSEDEFAULTCHANNEL";
            public bool Active = false;

            public override string MessageString => "**LevelUP Setup: Step 1.1:**\n"
                    + "\n"
                    + "Do you want to post advancement messages in a user's active channel? [Y/n]\n";

            public override SetupElement CreateInstance()
            {
                return new SetupElementUseDefaultChannel();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    Active = true;
                    index += 2;
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    Active = false;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want to post advancement messages in a user's active channel? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        private class SetupElementAdvancementChannel : SetupElement
        {
            public override string ID => "ADVANCEMENTCHANNEL";
            public ulong SelectedChannel;

            public override string MessageString => "**LevelUP Setup: Step 1.1.1:**\n"
                    + "\n"
                    + "Please select a channel in which to post user advancement messages.\n"
                    + "To select a channel, simply mention it in this channel.\n"
                    + "\n"
                    + "*Please mention a channel below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementAdvancementChannel();
            }

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
                            channel.SendMessageAsync("Selected channel '<#" + ch.Id + ">' (in '" + ch.Category.Name + "') as advancement channel.").GetAwaiter().GetResult();
                            break;
                        }
                    }
                }

                if (!found) {
                    channel.SendMessageAsync("Unable to find a channel with that name, or don't have acces to it, please check your message and try again.\n\nPlease select a channel in which to post user advancement messages.\nTo select a channel, simply mention it in this channel.").GetAwaiter().GetResult();
                }

                SelectedChannel = channelID;
                return found;
            }
        }

        private class SetupElementAdvancementMessage : SetupElement
        {
            public override string ID => "ADVANCEMENTMESSAGE";
            public string Template;

            public override string MessageString => "**LevelUP Setup: Step 1.2:**\n"
                    + "\n"
                    + "Please create a advancement message template, messages created with this template are posted in the advancements channel.\n"
                    + "\n"
                    + "Possible value replacements are:\n" 
                    + " - `%mention%` - replaced with the user mention\n"
                    + " - `%name%` - replaced with the user nickname\n"
                    + " - `%level%` - replaced with the current user level (after advancing)\n"
                    + " - `%currentxp%` - replaced with the remaining XP of the user (after advancing)\n"
                    + " - `%levelupxp%` - replaced with the max XP for advancing to the next level\n"
                    + "\n"
                    + "*Please enter a template below...*";

            public override SetupElement CreateInstance()
            {
                return new SetupElementAdvancementMessage();
            }

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {
                Template = msg.Content;
                if (!Template.Contains("%mention%")) {
                    channel.SendMessageAsync("**Warning:** `%mention%` is not present in the template, the user will not be pinged by the bot!").GetAwaiter().GetResult();
                }
                if (!Template.Contains("%level%")) {
                    channel.SendMessageAsync("**Warning:** `%level%` is not present in the template, the user wont see their new level!").GetAwaiter().GetResult();
                }
                channel.SendMessageAsync("Here follows a preview of the message:").GetAwaiter().GetResult();
                channel.SendMessageAsync(Template
                        .Replace("%mention%", "<@" + user.Id + ">")
                        .Replace("%name%", (user.Nickname == null || user.Nickname == "" ? user.Username : user.Nickname))
                        .Replace("%level%", "3")
                        .Replace("%currentxp%", "6")
                        .Replace("%levelupxp%", "3250")).GetAwaiter().GetResult();
                return true;
            }
        }

        private class SetupElementConfirm : SetupElement
        {
            private int goBackCount;
            public SetupElementConfirm(int goBackCount = 1) {
                this.goBackCount = goBackCount;
            }

            public override string ID => "CONFIRM";

            public override string MessageString => "Do you want to continue? [Y/n]";

            public override SetupElement CreateInstance() => new SetupElementConfirm(goBackCount);

            public override bool Process(SocketMessage msg, SocketTextChannel channel, SocketGuildUser user, SocketGuild guild, Server server, ConfigDictionary<string, object> mem, ref int index)
            {   
                if (msg.Content.ToLower() == "y" || msg.Content.ToLower() == "yes")
                {
                    return true;
                }
                if (msg.Content.ToLower() == "n" || msg.Content.ToLower() == "no")
                {
                    index -= 1 + goBackCount;
                    return true;
                }

                channel.SendMessageAsync("**Error:** expecting Yes/no or Y/n.\n\nDo you want to continue? [Y/n]").GetAwaiter().GetResult();
                return false;
            }
        }

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            ConfigDictionary<string, object> mem = module.serverMemory[server];

            if ((bool)mem.GetValueOrDefault("SetupRunning", false)) {
                await channel.SendMessageAsync("LevelUP setup has already been started by <@" + mem.GetValueOrDefault("SetupUser", -1) + ">, run " + GetBot().prefix + "cancel-levelup-setup to cancel.");
            } else {
                mem.Put("SetupChannel", channel.Id);
                mem.Put("SetupUser", user.Id);
                mem.Put("SetupRunning", true);

                List<SetupElement> setup = new List<SetupElement>(defaultSetup.Select(t => t.CreateInstance()));

                int index = 0;
                void startHandler(SetupElement ele) {
                    ele.Prepare(setup);
                    channel.SendMessageAsync(ele.MessageString).GetAwaiter().GetResult();
                    chatHandler(ele);
                }

                void chatHandler(SetupElement ele) {
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

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
        }
    }
}
