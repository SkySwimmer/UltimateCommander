using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CMDR;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Discord.Rest;

namespace Rolling {
    public class CreateMessageCommand : SystemCommand
    {
        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands") };

        public override string commandid => "create-role-message";

        public override string helpsyntax => "<template>";

        public override string description => "Creates a new role selection message";

        public override string permissionnode => "commands.admin.createrolemessage";

        public override bool setNoCmdPrefix => false;

        public override bool allowTerminal => false;

        public override bool allowDiscord => true;

        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            string content = arguments_string.TrimStart().Replace("\r", "");

            Dictionary<string, ulong> roles = new Dictionary<string, ulong>();
            foreach (string line in content.Split("\n")) {
                string d = line.Trim();
                if (d.StartsWith("- ")) {
                    d = d.Substring(2);
                }

                Match m = Regex.Match(d, "^(((\\:[a-zA-Z0-9]+\\:)|(\\<\\:[a-zA-Z0-9]+\\:[0-9]+\\>)|..?) (\\- )?(\\<\\@\\&[0-9]+\\>).*)$");
                if (m.Success) {
                    string emoji = m.Groups[2].Value;
                    string role = m.Groups[6].Value;

                    IEmote e = null;
                    try {
                        e = new Emoji(emoji);
                    } catch {
                        try {
                            e = Emote.Parse(emoji);
                        } catch {

                        }
                    }

                    if (e != null) {
                        ulong roleID = ulong.Parse(role.Substring(3).Remove(role.Substring(3).LastIndexOf(">")));
                        roles[emoji] = roleID;
                    }
                }
            }
           
            IMessage ms = channel.SendMessageAsync(content).GetAwaiter().GetResult();
            Message msg = new Message();
            msg.channel = ms.Channel.Id;
            msg.guild = guild.Id;
            msg.id = ms.Id;
            msg.message = channel.GetMessageAsync(msg.id).GetAwaiter().GetResult();
            foreach (string emoji in roles.Keys) {
                ulong role = roles[emoji];
                msg.roles.Add(role);

                IEmote e = null;
                try {
                    e = new Emoji(emoji);
                } catch {
                    e = Emote.Parse(emoji);
                }

                Message.MemberMeta mem = new Message.MemberMeta();
                mem.reactionIcon = e.Name;
                mem.role = role;
                msg.members.Add(mem);

                await ms.AddReactionAsync(e);
            }

            Module.module.roleMessages.Add(msg);
            Module.module.saveMessages();
            try {
                messageobject.DeleteAsync().GetAwaiter().GetResult();
            } catch {}
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
            throw new System.NotImplementedException();
        }
    }
}
