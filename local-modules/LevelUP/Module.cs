using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CMDR;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace levelup
{
    public class Module : BotModule
    {
        public override string id => "LevelUP";

        public override string moduledesctiption => "Level system for Discord";
        private Random rnd = new Random();

        public override void Init(Bot bot)
        {
        }

        public class UserLevel {
            public int Level = 0;
            public int TotalXP = 0;
            public int LevelUpXP = 0;
            public int CurrentXP = 0;
        }

        internal Dictionary<Server, ConfigDictionary<string, object>> serverMemory = new Dictionary<Server, ConfigDictionary<string, object>>();

        public override void PostInit(Bot bot)
        {
            foreach (Server srv in bot.servers) {
                Server.ModuleConfig conf = srv.GetModuleConfig(this);
                SocketGuild guild = bot.client.GetGuild(srv.id);

                if (conf.GetOrDefault("users", null) != null) {
                    List<ulong> users = Serializer.Deserialize<List<ulong>>(conf.GetOrDefault("users", null).ToString());

                    bool changed = false;
                    foreach (ulong user in users) {
                        if (guild.GetUser(user) == null) {
                            users.Remove(user);
                            conf.Set("user-" + user, null);
                            changed = true;
                        }
                    }
                    if (changed) {
                        conf.Set("users", Serializer.Serialize(users));
                    }
                }
            }

            bot.client.JoinedGuild += (guild) => {
                Server s = bot.GetServerFromSocketGuild(guild);
                while (s == null) {
                    s = bot.GetServerFromSocketGuild(guild);
                    Thread.Sleep(1);
                }
 
                serverMemory[s] = new ConfigDictionary<string, object>();
                return Task.CompletedTask;
            };
            bot.client.LeftGuild += (guild) => {
                foreach (Server srv in new List<Server>(serverMemory.Keys)) {
                    if (srv.id == guild.Id) {
                        serverMemory.Remove(srv);
                        break;
                    }
                }

                return Task.CompletedTask;
            };
            bot.client.UserLeft += (guild, user) => {
                Server srv = bot.GetServerFromSocketGuild(guild);
                Server.ModuleConfig conf = srv.GetModuleConfig(this);
                if (conf.GetOrDefault("users", null) != null) {
                    List<ulong> users = Serializer.Deserialize<List<ulong>>(conf.GetOrDefault("users", null).ToString());
                    if (users.Contains(user.Id)) {
                        users.Remove(user.Id);
                        conf.Set("user-" + user.Id, null);
                        conf.Set("users", Serializer.Serialize(users));
                    }
                }
                return Task.CompletedTask;
            };
            
            foreach (Server srv in GetServers()) {
                serverMemory[srv] = new ConfigDictionary<string, object>();
            }

            bot.client.MessageReceived += (message) => {
                if (message.Channel is SocketTextChannel) {
                    if (message.Author.IsBot)
                        return Task.CompletedTask;
                    if (message.Content.StartsWith(bot.prefix)) {
                        return Task.CompletedTask;
                    }
                    
                    SocketTextChannel ch = (SocketTextChannel)message.Channel;
                    Thread.Sleep(1000);

                    // Allow automod to remove spam
                    if (ch.GetMessageAsync(message.Id).GetAwaiter().GetResult() == null) {
                        return Task.CompletedTask;
                    }
                    
                    SocketGuild guild = ch.Guild;
                    Server srv = bot.GetServerFromSocketGuild(guild);
                    if (message.Content.StartsWith(srv.GetPrefix()))
                        return Task.CompletedTask;
                    Server.ModuleConfig conf = srv.GetModuleConfig(this);

                    int maxXPIncrease = (int)conf.GetOrDefault("xp.increase.max", 12);
                    int multiplier = (int)conf.GetOrDefault("xp.increase.multiplier.max", 10);

                    int defaultLevelBaseXP = (int)conf.GetOrDefault("xp.levelup.base", 1400);
                    int maxLevel = (int)conf.GetOrDefault("xp.maxlevel", 1000);

                    int xp = rnd.Next(1, multiplier) * (message.Content.Length > maxXPIncrease ? maxXPIncrease : message.Content.Length);

                    if (conf.GetOrDefault("user-" + message.Author.Id, null) == null) {
                        List<ulong> users = new List<ulong>();
                        if (conf.GetOrDefault("users", null) != null) {
                            users = Serializer.Deserialize<List<ulong>>(conf.GetOrDefault("users", null).ToString());
                        }
                        users.Add(message.Author.Id);
                        conf.Set("users", Serializer.Serialize(users));
                    }
                    string uL = (string)conf.GetOrDefault("user-" + message.Author.Id, null);
                    if (uL == null) {
                        uL = Serializer.Serialize(new UserLevel() {
                            LevelUpXP = defaultLevelBaseXP,
                            Level = 1
                        });
                    }
                    UserLevel level = Serializer.Deserialize<UserLevel>(uL);
                    
                    bool advanced = false;
                    level.TotalXP += xp;
                    level.CurrentXP += xp;

                    if (level.Level < maxLevel) {
                        while (level.CurrentXP > level.LevelUpXP) {
                            level.Level++;
                            level.CurrentXP -= level.LevelUpXP;
                            level.LevelUpXP = level.LevelUpXP + (level.LevelUpXP / 4);
                            advanced = true;
                        }
                    }

                    if (advanced) {
                        bool receivedRole = false;
                        if (conf.GetOrDefault("levelroles", null) != null) {
                            ConfigDictionary<int, ulong> levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());

                            SocketRole lastRole = null;
                            for (int i = 0; i <= level.Level; i++) {
                                if (levelRoles.ContainsKey(i)) {
                                    SocketRole role = guild.GetRole(levelRoles[i]);
                                    if (role != null) {
                                        SocketGuildUser usr = guild.GetUser(message.Author.Id);
                                        if (usr != null && usr.Roles.FirstOrDefault(t => t.Id == role.Id, null) == null) {
                                            lastRole = role;
                                            try {
                                                usr.AddRoleAsync(role.Id).GetAwaiter().GetResult();
                                                receivedRole = true;
                                            } catch {
                                            }
                                        }
                                    }
                                }
                            }

                            bool sendMessage = (bool)conf.GetOrDefault("messages.sendonroleadvancement", true);
                            if (sendMessage && lastRole != null) {
                                try {
                                    ulong channel = ch.Id;
                                    if (!(bool)conf.GetOrDefault("messages.roleadvancement.usecurrentchannel", true)) {
                                        channel = (ulong)conf.GetOrDefault("messages.roleadvancement.channel", ch.Id);
                                    }

                                    SocketTextChannel targetCh = guild.GetTextChannel(channel);
                                    if (targetCh == null)
                                        targetCh = ch;
                                    string messageContent = conf.GetOrDefault("messages.roleadvancement.template", "Congratulations %mention%! You advanced to level %level% and unlocked the %role% role!").ToString()
                                            .Replace("%mention%", "<@!" + message.Author.Id + ">")
                                            .Replace("%name%", (guild.GetUser(message.Author.Id).Nickname == null || guild.GetUser(message.Author.Id).Nickname == "" ? guild.GetUser(message.Author.Id).Username : guild.GetUser(message.Author.Id).Nickname))
                                            .Replace("%level%", level.Level.ToString())
                                            .Replace("%currentxp%", level.CurrentXP.ToString())
                                            .Replace("%levelupxp%", level.LevelUpXP.ToString())
                                            .Replace("%role%", "`" + lastRole.Name.ToString() + "`");
                                    targetCh.SendMessageAsync(messageContent).GetAwaiter().GetResult();
                                } catch {
                                    if ((bool)conf.GetOrDefault("messages.advancement.usecurrentchannel", true)) {
                                        try {
                                            SocketTextChannel targetCh = guild.DefaultChannel;
                                            string messageContent = conf.GetOrDefault("messages.roleadvancement.template", "Congratulations %mention%! You advanced to level %level% and unlocked the %role% role!").ToString()
                                                    .Replace("%mention%", "<@!" + message.Author.Id + ">")
                                                    .Replace("%name%", (guild.GetUser(message.Author.Id).Nickname == null || guild.GetUser(message.Author.Id).Nickname == "" ? guild.GetUser(message.Author.Id).Username : guild.GetUser(message.Author.Id).Nickname))
                                                    .Replace("%level%", level.Level.ToString())
                                                    .Replace("%currentxp%", level.CurrentXP.ToString())
                                                    .Replace("%levelupxp%", level.LevelUpXP.ToString())
                                                    .Replace("%role%", "`" + lastRole.Name.ToString() + "`");
                                            targetCh.SendMessageAsync(messageContent).GetAwaiter().GetResult();
                                        } catch {
                                        }
                                    }
                                }
                            } else {
                                receivedRole = false;
                            }
                        }
                        if (!receivedRole) {
                            bool sendMessage = (bool)conf.GetOrDefault("messages.sendonadvancement", true);
                            if (sendMessage) {
                                try {
                                    ulong channel = ch.Id;
                                    if (!(bool)conf.GetOrDefault("messages.advancement.usecurrentchannel", true)) {
                                        channel = (ulong)conf.GetOrDefault("messages.advancement.channel", ch.Id);
                                    }

                                    SocketTextChannel targetCh = guild.GetTextChannel(channel);
                                    if (targetCh == null)
                                        targetCh = ch;
                                    string messageContent = conf.GetOrDefault("messages.advancement.template", "Congratulations %mention%! You advanced to level %level%!").ToString()
                                            .Replace("%mention%", "<@!" + message.Author.Id + ">")
                                            .Replace("%name%", (guild.GetUser(message.Author.Id).Nickname == null || guild.GetUser(message.Author.Id).Nickname == "" ? message.Author.Username : guild.GetUser(message.Author.Id).Nickname))
                                            .Replace("%level%", level.Level.ToString())
                                            .Replace("%currentxp%", level.CurrentXP.ToString())
                                            .Replace("%levelupxp%", level.LevelUpXP.ToString());
                                    targetCh.SendMessageAsync(messageContent).GetAwaiter().GetResult();
                                } catch {
                                    if ((bool)conf.GetOrDefault("messages.advancement.usecurrentchannel", true)) {
                                        try {
                                            SocketTextChannel targetCh = guild.DefaultChannel;
                                            string messageContent = conf.GetOrDefault("messages.advancement.template", "Congratulations %mention%! You advanced to level %level%!").ToString()
                                                    .Replace("%mention%", "<@!" + message.Author.Id + ">")
                                                    .Replace("%name%", (guild.GetUser(message.Author.Id).Nickname == null || guild.GetUser(message.Author.Id).Nickname == "" ? message.Author.Username : guild.GetUser(message.Author.Id).Nickname))
                                                    .Replace("%level%", level.Level.ToString())
                                                    .Replace("%currentxp%", level.CurrentXP.ToString())
                                                    .Replace("%levelupxp%", level.LevelUpXP.ToString());
                                            targetCh.SendMessageAsync(messageContent).GetAwaiter().GetResult();
                                        } catch {
                                        }
                                    }
                                }
                            }
                        }
                    }
                    conf.Set("user-" + message.Author.Id, Serializer.Serialize(level));
                }
                return Task.CompletedTask;
            };
        }

        public override void PreInit(Bot bot)
        {
        }

        public override void RegisterCommands(Bot bot)
        {
            RegisterCommand(new SetupCommand(this));
            RegisterCommand(new CancelSetupCommand(this));
            RegisterCommand(new PruneAllLevelsCommand(this));
            RegisterCommand(new ResetUserLevelCommand(this));
            RegisterCommand(new ResetUserXPCommand(this));
            RegisterCommand(new SetUserLevelCommand(this));
            RegisterCommand(new ChangeOptionCommand(this));
            RegisterCommand(new ConfigureLevelRolesCommand(this));
            RegisterCommand(new UserLevelCommand(this));
        }
    }
}
