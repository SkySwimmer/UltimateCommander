using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMDR;
using Discord;
using Discord.WebSocket;

namespace levelup {
    public class UserLevelCommand : SystemCommand
    {
        private Module module;
        
        public UserLevelCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("levels", "Commands related to the level system") };
        public override string commandid => "level";
        public override string helpsyntax => "[user-mention]";
        public override string description => "shows your current level (or another user's level, for admins)";
        public override string permissionnode => "sys.anyone";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser usr, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments)
        {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            Server.ModuleConfig conf = server.GetModuleConfig(module);
            SocketGuildUser user = (SocketGuildUser)usr;

            EmbedBuilder builder = new EmbedBuilder()
                .WithColor(Color.Blue);

            if (arguments.Count != 0 && Bot.GetBot().CheckPermissions("commands.admin.levelup.getlevel.other", usr, guild)) {
                ulong id = 0;
                if (Regex.Match(arguments[0], "^\\<\\@![0-9]+\\>$").Success) {
                    id = ulong.Parse(arguments[0].Substring(3).Remove(arguments[0].Length - 4));
                } else {
                    try {
                        id = ulong.Parse(arguments[0]);
                    } catch {
                        var en = guild.GetUsersAsync().GetAsyncEnumerator();
                        while (true) {
                            if (en.Current != null) {
                                foreach (IGuildUser usr2 in en.Current) {
                                    if (usr2.DisplayName == arguments[0]) {
                                        id = usr2.Id;
                                    }
                                }
                            }
                            if (!en.MoveNextAsync().GetAwaiter().GetResult())
                                break;
                        }
                        if (id == 0) {
                            en = guild.GetUsersAsync().GetAsyncEnumerator();
                            while (true) {
                                if (en.Current != null) {
                                    foreach (IGuildUser usr2 in en.Current) {
                                        if (usr2.Nickname == arguments[0] && id == 0) {
                                            id = usr2.Id;
                                        }
                                    }
                                }
                                if (!en.MoveNextAsync().GetAwaiter().GetResult())
                                    break;
                            }
                        }
                    }
                }

                user = guild.GetUser(id);
                if (user == null) {
                    user = (SocketGuildUser)usr;
                } else {
                    builder = builder.WithColor(Color.Red);
                }
            }

            int maxLevel = (int)conf.GetOrDefault("xp.maxlevel", 1000);
            int defaultLevelBaseXP = (int)conf.GetOrDefault("xp.levelup.base", 1400);
            string uL = (string)conf.GetOrDefault("user-" + user.Id, null);
            if (uL == null) {
                uL = Serializer.Serialize(new Module.UserLevel() {
                    LevelUpXP = defaultLevelBaseXP,
                    Level = 1
                });
            }
            Module.UserLevel level = Serializer.Deserialize<Module.UserLevel>(uL);
            ConfigDictionary<int, ulong> levelRoles = new ConfigDictionary<int, ulong>();
            if (conf.GetOrDefault("levelroles", null) != null) {
                levelRoles = Serializer.Deserialize<ConfigDictionary<int, ulong>>(conf.GetOrDefault("levelroles", null).ToString());
            }

            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>(new EmbedFieldBuilder[] {
                new EmbedFieldBuilder().WithName("Current level").WithValue("Level " + level.Level).WithIsInline(true),
                new EmbedFieldBuilder().WithName("Current XP").WithValue(level.CurrentXP + "/" + level.LevelUpXP).WithIsInline(true)
            });
            if (level.Level < maxLevel) {
                fields.Add(new EmbedFieldBuilder().WithName("Next level").WithValue("Level " + (level.Level + 1) + " (" + (level.LevelUpXP - level.CurrentXP) + " XP needed)").WithIsInline(true));
            }

            string progressDescription = "```\n[";
            double max = 18;
            double step = 18d / (double)level.LevelUpXP;
            int value = (int)(step * level.CurrentXP);
            for (int i = 0; i < value; i++) {
                progressDescription += "=";
            }
            for (int i = value; i < max; i++) {
                progressDescription += " ";
            }
            progressDescription += "] " + level.CurrentXP + "/" + level.LevelUpXP + "\n```";

            builder = builder.WithTitle("Level of `" + (user.Nickname == null ? user.DisplayName : user.Nickname) + "`:")
                .WithFields(fields)
                .WithDescription(progressDescription)
                .WithThumbnailUrl((user.GetAvatarUrl() == "" || user.GetAvatarUrl() == null ? user.GetDefaultAvatarUrl() : user.GetAvatarUrl()));
            if (new List<int>(levelRoles.OrderBy(t => t.Key).Where(t => level.Level < t.Key).Where(t => guild.GetRole(t.Value) != null).Select(t => t.Key)).Count != 0) {
                int lv = new List<int>(levelRoles.OrderBy(t => t.Key).Where(t => level.Level < t.Key).Where(t => guild.GetRole(t.Value) != null).Select(t => t.Key))[0];
                fields.Add(new EmbedFieldBuilder().WithName("Next role").WithValue(guild.GetRole(levelRoles[lv]).Name + " (level " + lv + ")"));
                builder.WithFooter("Next role: " + guild.GetRole(levelRoles[lv]).Name + " (level " + lv + ")");
            }
            
            await channel.SendMessageAsync("", false, builder.Build());
        }

        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments)
        {
        }
    }
}