using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CMDR;
using Discord;
using Discord.WebSocket;

namespace Rolling
{
    public class Module : BotModule
    {
        public static Module module;

        public override string id => "RollingModule";

        public override string moduledesctiption => "Module for creature role-assignment messages";

        public List<Message> roleMessages = new List<Message>();

        public override void Init(Bot bot)
        {
            module = this;
            String doc = GetConfig().GetValueOrDefault("roleMessages", Serializer.Serialize(roleMessages)).ToString();
            roleMessages = Serializer.Deserialize<List<Message>>(doc);

            bool changed = false;
            List<Message> lst = new List<Message>(roleMessages);
            foreach (Message msg in lst) {
                SocketGuild guild = bot.client.GetGuild(msg.guild);
                if (guild == null || guild.GetTextChannel(msg.channel) == null || guild.GetTextChannel(msg.channel).GetMessageAsync(msg.id).GetAwaiter().GetResult() == null) {
                    roleMessages.Remove(msg);
                    changed = true;
                } else {
                   msg.message = guild.GetTextChannel(msg.channel).GetMessageAsync(msg.id).GetAwaiter().GetResult();
                }
            }

            bot.client.MessageDeleted += handleDeletion;            
            if (changed) {
                saveMessages();
            }
        }

        private Task handleDeletion(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel) {
            if (channel.Value is SocketTextChannel) {
                SocketTextChannel tc = (SocketTextChannel)channel.Value;
                SocketGuild g = tc.Guild;
                List<Message> lst = new List<Message>(roleMessages);
                foreach (Message ms in lst) {
                    if (ms.guild == g.Id && ms.channel == tc.Id && ms.id == msg.Id) {
                        roleMessages.Remove(ms);
                        saveMessages();
                        break;
                    }
                }
            }
            return Task.CompletedTask;
        }

        public void saveMessages() {
            GetConfig().Put("roleMessages", Serializer.Serialize(roleMessages));
            SaveConfig();
        }

        public override void PostInit(Bot bot)
        {
            bool changed = false;
            foreach (Message msg in roleMessages) {
                if (msg.message != null) {
                    foreach (Message.MemberMeta roleD in msg.members) {
                        IEmote e = null;
                        try {
                            e = new Emoji(roleD.reactionIcon);
                        } catch {
                            e = Emote.Parse(roleD.reactionIcon);
                        }
                        bool found = false;
                        ReactionMetadata data = default(ReactionMetadata);
                        foreach (IEmote e2 in msg.message.Reactions.Keys) {
                            if (e2.Name == e.Name) {
                                found = true;
                                data = msg.message.Reactions[e2];
                                break;
                            }
                        }

                        if (!found) {
                            msg.message.AddReactionAsync(e).GetAwaiter().GetResult();
                            roleD.users.Clear();
                            changed = true;
                        } else {
                            var en = msg.message.GetReactionUsersAsync(e, data.ReactionCount).GetAsyncEnumerator();
                            Dictionary<ulong, IUser> rUsers = new Dictionary<ulong, IUser>();
                            while (true) {
                                if (en.Current != null) {
                                    foreach (IUser usr in en.Current) {
                                        if (!rUsers.ContainsKey(usr.Id) && !usr.IsBot)
                                            rUsers[usr.Id] = usr;
                                    }
                                }
                                if (!en.MoveNextAsync().GetAwaiter().GetResult())
                                    break;
                            }

                            SocketGuild guild = bot.client.GetGuild(msg.guild);
                            foreach (ulong usr in new List<ulong>(roleD.users)) {
                                if (!rUsers.ContainsKey(usr)) {
                                    changed = true;
                                    roleD.users.Remove(usr);
                                    SocketGuildUser user = guild.GetUser(usr);
                                    if (user != null) {
                                        if (user.Roles.FirstOrDefault(t => t.Id == roleD.role, null) != null) {
                                            user.RemoveRoleAsync(roleD.role).GetAwaiter().GetResult();
                                        }
                                    }
                                }
                            }
                            foreach (ulong usr in rUsers.Keys) {
                                if (!roleD.users.Contains(usr)) {
                                    changed = true;
                                    roleD.users.Add(usr);
                                    SocketGuildUser user = guild.GetUser(usr);
                                    if (user != null) {
                                        if (user.Roles.FirstOrDefault(t => t.Id == roleD.role, null) == null) {
                                            user.AddRoleAsync(roleD.role).GetAwaiter().GetResult();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (changed)
                saveMessages();
            changed = false;

            bot.client.ReactionAdded += new Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task>((message, ch, reaction) => {
                foreach (Message msg in roleMessages) {
                    if (msg.message != null && msg.channel == ch.Id && msg.id == message.Id) {
                        foreach (Message.MemberMeta roleD in msg.members) {
                            IEmote e = null;
                            try {
                                e = new Emoji(roleD.reactionIcon);
                            } catch {
                                e = Emote.Parse(roleD.reactionIcon);
                            }

                            if (e.Name == reaction.Emote.Name) {
                                ulong usr = reaction.UserId;
                                SocketGuild guild = bot.client.GetGuild(msg.guild);
                                SocketGuildUser user = guild.GetUser(usr);
                                if (user != null) {
                                    if (user.Roles.FirstOrDefault(t => t.Id == roleD.role, null) == null) {
                                        user.AddRoleAsync(roleD.role).GetAwaiter().GetResult();
                                    }
                                }
                                GetConfig().Put("roleMessages", Serializer.Serialize(roleMessages));
                                changed = true;
                                break;
                            }
                        }
                    }
                }
                return null;
            });
            bot.client.ReactionRemoved += new Func<Cacheable<IUserMessage, ulong>, Cacheable<IMessageChannel, ulong>, SocketReaction, Task>((message, ch, reaction) => {
                foreach (Message msg in roleMessages) {
                    if (msg.message != null && msg.channel == ch.Id && msg.id == message.Id) {
                        foreach (Message.MemberMeta roleD in msg.members) {
                            IEmote e = null;
                            try {
                                e = new Emoji(roleD.reactionIcon);
                            } catch {
                                e = Emote.Parse(roleD.reactionIcon);
                            }

                            if (e.Name == reaction.Emote.Name) {
                                ulong usr = reaction.UserId;
                                SocketGuild guild = bot.client.GetGuild(msg.guild);
                                SocketGuildUser user = guild.GetUser(usr);
                                if (user != null) {
                                    if (user.Roles.FirstOrDefault(t => t.Id == roleD.role, null) != null) {
                                        user.RemoveRoleAsync(roleD.role).GetAwaiter().GetResult();
                                    }
                                }
                                GetConfig().Put("roleMessages", Serializer.Serialize(roleMessages));
                                changed = true;
                                break;
                            }
                        }
                    }
                }
                return null;
            });

            new Thread(() => {
                while (true) {
                    if (changed)
                        SaveConfig();
                    changed = false;
                    Thread.Sleep(60 * 5 * 1000);
                }
            }).Start();
        }

        public override void PreInit(Bot bot)
        {
        }

        public override void RegisterCommands(Bot bot)
        {
            RegisterCommand(new CreateMessageCommand());
        }
    }
}
