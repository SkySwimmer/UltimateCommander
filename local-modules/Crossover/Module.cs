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
        // Cache for user mutual servers
        public Dictionary<ulong, UserInfo> UserCache = new Dictionary<ulong,UserInfo>();
    
        public override string id => "Crossover";

        public override string moduledesctiption => "A CMD-R module for creating crossover roles (roles that are given when members are in specific servers)";

        public override void Init(Bot bot)
        {
        }

        public override void PostInit(Bot bot)
        {
            string status = bot.client.Activity.Details;

            // Set status
            bot.client.SetGameAsync("Loading Crossover...").GetAwaiter().GetResult();
            
            // Bind events
            bot.client.UserJoined += (user) => {
                if (user.IsBot)
                    return Task.CompletedTask;
                // Add user if not present
                SocketGuild guild = user.Guild;
                if (!UserCache.ContainsKey(user.Id))
                    UserCache[user.Id] = new UserInfo()
                    {
                        Id = user.Id,
                        Servers = new Dictionary<ulong, UserServerInfo>()
                    };

                // Add guild to user guild list
                if (!UserCache[user.Id].Servers.ContainsKey(guild.Id))
                    UserCache[user.Id].Servers[guild.Id] = new UserServerInfo() {
                        Id = guild.Id,
                        Roles = new List<ulong>()
                    };

                // Sync guild roles
                AddRolesForUserIn(UserCache[user.Id].Servers[guild.Id], user, false);

                // For in case the user re-joined the server
                UserServerInfo[] servers = UserCache[user.Id].Servers.Values.ToArray();
                foreach (UserServerInfo server in servers) 
                {
                    // Handle role sync
                    if (server.Id != guild.Id) {
                        AddRolesForUserIn(server, bot.client.GetGuild(server.Id).GetUser(user.Id), true);
                    }
                }
                return Task.CompletedTask;
            };
            bot.client.UserLeft += (guild, user) => {// Sync guild roles
                // Remove from server
                UserServerInfo[] servers = UserCache[user.Id].Servers.Values.ToArray();
                foreach (UserServerInfo server in servers) 
                {
                    // Handle role sync
                    if (server.Id != guild.Id) {
                        RemoveRolesForUserIn(server, bot.client.GetGuild(server.Id).GetUser(user.Id));
                    }
                }

                // Remove user from guild
                while (true)
                {
                    try
                    {
                        ulong id = user.Id;
                        if (UserCache[id].Servers.ContainsKey(guild.Id))
                            UserCache[id].Servers.Remove(guild.Id);
                        if (UserCache[id].Servers.Count == 0)
                        {
                            // Keeping this user in cache will cause a memory leak we should remove it
                            UserCache.Remove(id);
                        }
                        break;
                    }
                    catch
                    {
                    }
                }
                return Task.CompletedTask;
            };
            bot.client.JoinedGuild += (guild) => {
                SyncGuild(guild);
                foreach (Server srv in bot.servers) {
                    loadServer(srv);
                }
                return Task.CompletedTask;
            };
            bot.client.LeftGuild += (guild) => {
                // Remove roles from users
                while (true) {
                    try {
                        foreach (ulong id in UserCache.Keys)
                            if (UserCache[id].Servers.ContainsKey(guild.Id))
                            {
                                // Remove from server
                                UserServerInfo[] servers = UserCache[id].Servers.Values.ToArray();
                                foreach (UserServerInfo server in servers) 
                                {
                                    // Handle role sync
                                    if (server.Id != guild.Id) {
                                        RemoveRolesForUserIn(server, bot.client.GetGuild(server.Id).GetUser(id));
                                    }
                                }
                            }
                        break;
                    } catch {
                    }
                }

                // Remove from guild memory
                while (true) {
                    try {
                        foreach (ulong id in UserCache.Keys)
                        {
                            if (UserCache[id].Servers.ContainsKey(guild.Id))
                                UserCache[id].Servers.Remove(guild.Id);
                            if (UserCache[id].Servers.Count == 0)
                            {
                                // Keeping this user in cache will cause a memory leak we should remove it
                                UserCache.Remove(id);
                            }
                        }
                        break;
                    } catch {
                    }
                }
                return Task.CompletedTask;
            };
            bot.client.GuildMemberUpdated += (oldUser, user) => {
                // Handle new roles
                // Add user if not present
                SocketGuild guild = user.Guild;
                if (!UserCache.ContainsKey(user.Id))
                    UserCache[user.Id] = new UserInfo()
                    {
                        Id = user.Id,
                        Servers = new Dictionary<ulong, UserServerInfo>()
                    };

                // Add guild to user guild list
                if (!UserCache[user.Id].Servers.ContainsKey(guild.Id))
                    UserCache[user.Id].Servers[guild.Id] = new UserServerInfo() {
                        Id = guild.Id,
                        Roles = new List<ulong>()
                    };
                UserCache[user.Id].Servers[guild.Id].Roles = user.Roles.ToArray().Select(t => t.Id).ToList();

                // Role sync
                AddRolesForUserIn(UserCache[user.Id].Servers[guild.Id], user, true);
                RemoveRolesForUserIn(UserCache[user.Id].Servers[guild.Id], user);
                UserServerInfo[] servers = UserCache[user.Id].Servers.Values.ToArray();
                foreach (UserServerInfo server in servers) 
                {
                    // Handle role sync
                    if (server.Id != guild.Id) {
                        AddRolesForUserIn(server, bot.client.GetGuild(server.Id).GetUser(user.Id), true);
                        RemoveRolesForUserIn(server, bot.client.GetGuild(server.Id).GetUser(user.Id));
                    }
                }
                return Task.CompletedTask;
            };

            // Load servers
            foreach (Server srv in GetBot().servers) {
                SocketGuild guild = GetBot().client.GetGuild(srv.id);
                
                SyncGuild(guild);
                loadServer(srv);
            }
            bot.client.SetGameAsync(status).GetAwaiter().GetResult();
        }

        void SyncGuild(SocketGuild guild)
        {
            foreach (IUser usr in guild.Users)
            {
                if (usr is SocketGuildUser)
                {
                    // Add user if not present
                    SocketGuildUser user = (SocketGuildUser)usr;
                    if (!UserCache.ContainsKey(user.Id))
                        UserCache[user.Id] = new UserInfo()
                        {
                            Id = user.Id,
                            Servers = new Dictionary<ulong, UserServerInfo>()
                        };

                    // Add guild to user guild list
                    var roles = user.Roles.ToArray();
                    if (!UserCache[user.Id].Servers.ContainsKey(guild.Id))
                        UserCache[user.Id].Servers[guild.Id] = new UserServerInfo()
                        {
                            Id = guild.Id,
                            Roles = roles.Select(t => t.Id).ToList()
                        };
                }
            }
        }

        // Called to remove roles from a user in a server
        private void RemoveRolesForUserIn(UserServerInfo server, SocketGuildUser user)
        {
            // Retrieve server object
            Server srv = GetBot().servers.FirstOrDefault(t => t.id == server.Id);
            if (srv == null || user == null)
                return; // What-
            
            // Load configs
            var conf = srv.GetModuleConfig(this);
            ConfigDictionary<ulong, List<ulong>> roleConfig = DeserializeRoles(conf.GetOrDefault("roles", "<ConfigDictionary />").ToString());
            ConfigDictionary<string, ulong> filter = Serializer.Deserialize<ConfigDictionary<string, ulong>>(conf.GetOrDefault("roleFilters", "<ConfigDictionary />").ToString());

            // Check if the server has crossover roles
            if (roleConfig.Count == 0)
                return; // Okay.... this server removed all the crossover roles but kept the bot, or hasn't set it up, lets skip and reduce load

            // Check mutual servers and roles
            UserServerInfo[] servers = UserCache[user.Id].Servers.Values.ToArray();
            foreach (ulong roleServer in roleConfig.Keys)
            {
                if (servers.FirstOrDefault(t => t.Id == roleServer) == null)
                {
                    // Remove the roles
                    foreach (ulong role in roleConfig[roleServer])
                    {
                        if (server.Roles.Contains(role))
                        {
                            try
                            {
                                user.RemoveRoleAsync(role).GetAwaiter().GetResult();
                            }
                            catch
                            {
                                return;
                            }
                        }
                    }
                }
                else 
                {
                    // Check roles with filters
                    foreach (ulong role in roleConfig[roleServer])
                    {
                        if (server.Roles.Contains(role))
                        {
                            if (filter.ContainsKey(roleServer + "-" + role))
                            {
                                // Remove the role if the required role is not present in the second server
                                ulong targetRole = filter[roleServer + "-" + role];
                                if (targetRole != 0)
                                {
                                    UserServerInfo info = UserCache[user.Id].Servers[roleServer];
                                    if (!info.Roles.Contains(targetRole))
                                    {
                                        try
                                        {
                                            user.RemoveRoleAsync(role).GetAwaiter().GetResult();
                                        }
                                        catch
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Called to add roles in a server to a user
        private void AddRolesForUserIn(UserServerInfo server, SocketGuildUser user, bool filters)
        {
            // Retrieve server object
            Server srv = GetBot().servers.FirstOrDefault(t => t.id == server.Id);
            if (srv == null || user == null || user.IsBot)
                return; // What-
            
            // Load configs
            var conf = srv.GetModuleConfig(this);
            ConfigDictionary<ulong, List<ulong>> roleConfig = DeserializeRoles(conf.GetOrDefault("roles", "<ConfigDictionary />").ToString());
            ConfigDictionary<string, ulong> filter = Serializer.Deserialize<ConfigDictionary<string, ulong>>(conf.GetOrDefault("roleFilters", "<ConfigDictionary />").ToString());

            // Check if the server has crossover roles
            if (roleConfig.Count == 0)
                return; // Okay.... this server removed all the crossover roles but kept the bot, or hasn't set it up, lets skip and reduce load

            // Check mutual servers
            UserServerInfo[] servers = UserCache[user.Id].Servers.Values.ToArray();
            foreach (UserServerInfo mutualServer in servers) {
                if (mutualServer.Id != server.Id) {
                    // Find crossover roles for this server
                    if (roleConfig.ContainsKey(mutualServer.Id)) {
                        // Alright lets try sync

                        // Load roles
                        ulong[] roles = roleConfig[mutualServer.Id].ToArray();
                        if (roles.Length == 0)
                            continue; // alright empty config -_-

                        // Add roles
                        foreach (ulong role in roles) {
                            // If the role has a filter and filtering is disabled we should skip it
                            if (!filters && filter.ContainsKey(mutualServer.Id + "-" + role))
                                continue;

                            // Skip role if the user has it
                            if (server.Roles.Contains(role))
                                continue;

                            // Check filters
                            if (filters && filter.ContainsKey(mutualServer.Id + "-" + role))
                            {
                                ulong targetRole = filter[mutualServer.Id + "-" + role];
                                if (targetRole != 0)
                                    if (!mutualServer.Roles.Contains(targetRole))
                                        continue;
                            }

                            // Add the role
                            try {
                                user.AddRoleAsync(role).GetAwaiter().GetResult();
                            } catch {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void loadServer(Server srv) {
            SocketGuild guild = GetBot().client.GetGuild(srv.id);
            if (guild != null) {
                foreach (IUser usr in guild.Users) {
                    if (usr is SocketGuildUser) {
                        SocketGuildUser user = (SocketGuildUser)usr;

                        // Role sync
                        AddRolesForUserIn(UserCache[user.Id].Servers[guild.Id], user, true);
                        RemoveRolesForUserIn(UserCache[user.Id].Servers[guild.Id], user);
                        UserServerInfo[] servers = UserCache[user.Id].Servers.Values.ToArray();
                        foreach (UserServerInfo server in servers) 
                        {
                            // Handle role sync
                            if (server.Id != guild.Id) {
                                AddRolesForUserIn(server, GetBot().client.GetGuild(server.Id).GetUser(user.Id), true);
                                RemoveRolesForUserIn(server, GetBot().client.GetGuild(server.Id).GetUser(user.Id));
                            }
                        }
                    }
                }
            }
        }

        public override void PreInit(Bot bot)
        {
        }

        public override void RegisterCommands(Bot bot)
        {
            RegisterCommand(new GetCurrentGuildIDCommand());
            RegisterCommand(new RoleConfigurationCommand(this));
        }


        //
        // Role utilities
        //

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
