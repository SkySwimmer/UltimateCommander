using CMDR;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SubObuscate;

namespace link_r {
    public class LoginCommand : SystemCommand, DmSupportedCommand {
        private Module module;
        
        public LoginCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "linkr-login";
        public override string helpsyntax => "";
        public override string description => "generates a Link/R login URL";
        public override string permissionnode => "sys.anyone";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => false;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser user, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            try {
                IDMChannel dm = user.CreateDMChannelAsync().GetAwaiter().GetResult();
                IntentRunner runner = IntentRunner.Spin(guild.Id, user.Id, "Login", new LoginIntent.StartParameters() {
                    guild = guild.Id,
                    discordUserId = user.Id
                });            
                string website = module.GetConfig().GetValueOrDefault("website", "https://aerialworks.ddns.net/linkr").ToString() + "/login?user=" + user.Id
                    + "&guild=" + guild.Id
                    + "&subsystemaddress=" + runner.GetSubsystemAddress()
                    + "&intenttoken=" + runner.GetIntentToken()
                    + "&bot=" + Bot.GetBot().client.CurrentUser.Id;
                await dm.SendMessageAsync("**Link/R Login URL:**\n**" + website + "**\n\n**Expires in:** *15 minutes.*\n**Do not share this URL, it grants access to your Link/R account.**");
            } catch {
                channel.SendMessageAsync("An error occured while trying to create a login URL for <@" + user.Id + ">, please report this to an admin.").GetAwaiter().GetResult();
            }
        }

        public async Task OnExecuteFromDM(SocketUser user, SocketDMChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            IntentRunner runner = IntentRunner.Spin(0, user.Id, "Login", new LoginIntent.StartParameters() {
                guild = 0,
                discordUserId = user.Id
            });
            
            string website = module.GetConfig().GetValueOrDefault("website", "aerialworks.ddns.net/linkr").ToString() + "/login?user=" + user.Id
                + "&subsystemaddress=" + runner.GetSubsystemAddress()
                + "&intenttoken=" + runner.GetIntentToken()
                + "&bot=" + Bot.GetBot().client.CurrentUser.Id;
            await channel.SendMessageAsync("**Link/R Login URL:**\n**" + website + "**\n\n**Expires in:** *15 minutes.*\n**Do not share this URL, it grants access to your Link/R account.**");
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
        }
    }
}
