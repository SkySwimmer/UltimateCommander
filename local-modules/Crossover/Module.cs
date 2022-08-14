using System;
using System.Collections.Generic;
using CMDR;
using Discord;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace crossover
{
    public class Module : BotModule
    {
        public override string id => "Crossover";

        public override string moduledesctiption => "A CMD-R module for creating crossover roles (roles that are given when members are in specific servers)";

        public override void Init(Bot bot)
        {
        }

        public override void PostInit(Bot bot)
        {
            string status = bot.client.Activity.Details;
            bot.client.SetGameAsync("Loading Crossover...").GetAwaiter().GetResult();
            bot.client.UserJoined += (user) => {
                foreach (SocketGuild g in user.MutualGuilds) {
                    loadUser(user.Id, g, bot.GetServerFromSocketGuild(g));
                }
                SocketGuild guild = user.Guild;
                loadUser(user.Id, guild, bot.GetServerFromSocketGuild(guild));
                return Task.CompletedTask;
            };
            bot.client.UserLeft += (guild, user) => {
                foreach (SocketGuild g in user.MutualGuilds) {
                    loadUser(user.Id, g, bot.GetServerFromSocketGuild(g));
                }
                return Task.CompletedTask;
            };
            bot.client.JoinedGuild += (guild) => {
                foreach (Server srv in bot.servers) {
                    loadServer(srv);
                }
                return Task.CompletedTask;
            };
            bot.client.LeftGuild += (guild) => {
                foreach (Server srv in GetBot().servers) {
                    loadServer(srv);
                }
                return Task.CompletedTask;
            };
            bot.client.GuildMemberUpdated += (oldUser, user) => {
                foreach (SocketGuild g in user.MutualGuilds) {
                    loadUser(user.Id, g, bot.GetServerFromSocketGuild(g));
                }
                SocketGuild guild = user.Guild;
                loadUser(user.Id, guild, bot.GetServerFromSocketGuild(guild));
                return Task.CompletedTask;
            };
			bot.client.SetGameAsync(status).GetAwaiter().GetResult();
        }

        public override void PreInit(Bot bot)
        {
        }

        public override void RegisterCommands(Bot bot)
        {
            RegisterCommand(new GetCurrentGuildIDCommand());
            RegisterCommand(new RoleConfigurationCommand(this));
        }

        public void loadServer(Server srv) {
            SocketGuild guild = GetBot().client.GetGuild(srv.id);
            if (guild != null) {
                foreach (IUser usr in guild.Users) {
                    if (usr is SocketGuildUser) {
                        SocketGuildUser user = (SocketGuildUser)usr;
                        loadUser(user.Id, guild, srv);
                    }
                }
            }
        }

        private void loadUser(ulong userID, SocketGuild guild, Server srv)
        {
            SocketGuildUser user = guild.GetUser(userID);
            if (user == null || user.IsBot)
                return;
            var conf = srv.GetModuleConfig(this);
            ConfigDictionary<ulong, List<ulong>> roleConfig = DeserializeRoles(conf.GetOrDefault("roles", "<ConfigDictionary />").ToString());
            ConfigDictionary<string, ulong> filter = Serializer.Deserialize<ConfigDictionary<string, ulong>>(conf.GetOrDefault("roleFilters", "<ConfigDictionary />").ToString());
                                
            foreach (ulong server in roleConfig.Keys) {
                List<ulong> roles = roleConfig[server];
                if (user.MutualGuilds.FirstOrDefault(t => t.Id == server, null) != null) {
                    SocketGuildUser uD = user.MutualGuilds.FirstOrDefault(t => t.Id == server, null).GetUser(user.Id);
                    foreach (ulong role in roles) {
                        if (filter.ContainsKey(server + "-" + role)) {
                            ulong rd = filter.GetValueOrDefault(server + "-" + role, (ulong)0);
                            if (rd != 0) {
                                if (uD.Roles.FirstOrDefault(t => t.Id == rd, null) == null) {
                                    if (user.Roles.FirstOrDefault(t => t.Id != role, null) != null) {
                                        try {
                                            user.RemoveRoleAsync(role).GetAwaiter().GetResult();
                                        } catch {
                                        }
                                    }
                                    
                                    continue;
                                }
                            }
                        }

                        if (user.Roles.FirstOrDefault(t => t.Id == role, null) == null) {
                            try {
                                user.AddRoleAsync(role).GetAwaiter().GetResult();
                            } catch (Exception e) {
                            }
                        }
                    }
                } else {
                    foreach (ulong role in roles) {
                        if (user.Roles.FirstOrDefault(t => t.Id != role, null) != null) {
                            try {
                                user.RemoveRoleAsync(role).GetAwaiter().GetResult();
                            } catch {
                            }
                        }
                    }
                }
            }
        }

        public string SerializeRoles(ConfigDictionary<ulong, List<ulong>> roleConfig)
        {
            ConfigDictionary<ulong, string> map = new ConfigDictionary<ulong, string>();
            foreach (ulong server in roleConfig.Keys) {
                map[server] = Serializer.Serialize(roleConfig[server]);
            }
            return Serializer.Serialize(map);
        }

        public ConfigDictionary<ulong, List<ulong>> DeserializeRoles(string data)
        {
            ConfigDictionary<ulong, List<ulong>> roles = new ConfigDictionary<ulong, List<ulong>>();
            ConfigDictionary<ulong, string> map = Serializer.Deserialize<ConfigDictionary<ulong, string>>(data);

            foreach (ulong server in map.Keys) {
                roles[server] = Serializer.Deserialize<List<ulong>>(map[server]);
            }

            return roles;
        }
    }
}
