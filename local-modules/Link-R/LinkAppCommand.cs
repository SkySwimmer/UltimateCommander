using CMDR;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using SubObuscate;
using System.Text;

namespace link_r {
    public class LinkAppCommand : SystemCommand {
        private Module module;
        
        public LinkAppCommand(Module module) {
            this.module = module;
        }

        public override CmdCategory[] Categories => new CmdCategory[] { new CmdCategory("utility", "Utility commands"), new CmdCategory("verification", "Verification commands") };
        public override string commandid => "linkr-app-links";
        public override string helpsyntax => "[list/unlink] [app-id]";
        public override string description => "adds or removes application links (use no arguments to link a app)";
        public override string permissionnode => "commands.admin.linkr.linkapp";
        public override bool setNoCmdPrefix => false;
        public override bool allowTerminal => true;
        public override bool allowDiscord => true;
        public override async Task OnExecuteFromDiscord(SocketGuild guild, SocketUser usr, SocketTextChannel channel, SocketMessage messageobject, string fullmessage, string arguments_string, List<string> arguments) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            Server.ModuleConfig conf = server.GetModuleConfig(module);

            if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                if (arguments.Count == 0) {
                    try {
                        IntentRunner runner = IntentRunner.Spin(guild.Id, 0, "Link");                    
                        await channel.SendMessageAsync("Application link is ready to be used, here are the connection details:\n\n" 
                            + "```\n"
                            + "Subsystem Domain: " + runner.GetDomain() + "\n"
                            + "Subsystem Subdomain: " + runner.GetSubdomain() + "\n"
                            + "Subsystem Address: " + runner.GetSubsystemAddress() + "\n"
                            + "Subsystem Intent Token: " + runner.GetIntentToken() + "\n"
                            + "```\n"
                            + "**Note:** this subsystem will shut down if not used within 15 minutes.");
                    } catch {
                        await channel.SendMessageAsync("Failed to start the Application Linking Subsystem!");
                    }
                } else if (arguments[0].Equals("list")) {
                    List<string> links = new List<string>();
                    List<string> apps = new List<string>();
                    if (conf.Get("applications") != null) {
                        apps = Serializer.Deserialize<List<string>>(conf.Get("applications").ToString());
                    }
                    foreach (string app in apps) {
                        if (conf.Get("app-" + app) == null)
                            continue;
                        
                        string token = conf.Get("app-" + app).ToString();
                        string header = token.Split(".")[0];
                        string payload = token.Split(".")[1];
                        
                        string payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(payload));
                        string headerJson = Encoding.UTF8.GetString(Base64Url.Decode(header));
                        Dictionary<string, object> info = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);

                        links.Add(info["appname"] + ": " + app);
                    }
                    
                    string message = "List of linked applications:\n```\n";
                    foreach (string line in links) {
                        message += " - " + line + "\n";
                    }
                    message += "```";
                    await channel.SendMessageAsync(message);
                } else if (arguments[0].Equals("unlink") && arguments.Count >= 2) {
                    List<string> apps = new List<string>();
                    if (conf.Get("applications") != null) {
                        apps = Serializer.Deserialize<List<string>>(conf.Get("applications").ToString());
                    }
                    if (apps.Contains(arguments[1])) {
                        apps.Remove(arguments[1]);
                        conf.Set("app-" + arguments[1], null);
                        conf.Set("applications", Serializer.Serialize(apps));
                        await channel.SendMessageAsync("Application has been unlinked.");
                    } else {
                        await channel.SendMessageAsync("Application not found.");
                    }
                } else {
                    await channel.SendMessageAsync("Invalid usage.");
                }
            } else {
                await channel.SendMessageAsync("Link/R setup has not been completed, please run `setup-linkr` first.");
            }
        }
        
        public override void OnExecuteFromTerminal(string fullcommand, string arguments_string, List<string> arguments) {
            if (arguments.Count == 0) {
                try {
                    IntentRunner runner = IntentRunner.Spin(0, 0, "Link");                    
                    Bot.WriteLine("Application link is ready to be used, here are the connection details:\n"
                        + "Subsystem Domain: " + runner.GetDomain() + "\n"
                        + "Subsystem Subdomain: " + runner.GetSubdomain() + "\n"
                        + "Subsystem Address: " + runner.GetSubsystemAddress() + "\n"
                        + "Subsystem Intent Token: " + runner.GetIntentToken() + "\n"
                        + "\n"
                        + "Note: this subsystem will shut down if not used within 15 minutes.");
                } catch {
                    Bot.WriteLine("Failed to start the Application Linking Subsystem!");
                }
            } else if (arguments[0].Equals("list")) {
                List<string> links = new List<string>();
                List<string> apps = new List<string>();
                if (module.GetConfig().GetValueOrDefault("applications", null) != null) {
                    apps = Serializer.Deserialize<List<string>>(module.GetConfig().GetValue("applications").ToString());
                }
                foreach (string app in apps) {
                    if (module.GetConfig().GetValueOrDefault("app-" + app, null) == null)
                        continue;
                    
                    string token = module.GetConfig().GetValue("app-" + app).ToString();
                    string header = token.Split(".")[0];
                    string payload = token.Split(".")[1];
                    
                    string payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(payload));
                    string headerJson = Encoding.UTF8.GetString(Base64Url.Decode(header));
                    Dictionary<string, object> info = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);

                    links.Add(info["appname"] + ": " + app);
                }
            
                string message = "List of linked applications:";
                foreach (string line in links) {
                    message += "\n - " + line;
                }
                Bot.WriteLine(message);
            } else if (arguments[0].Equals("unlink") && arguments.Count >= 2) {
                List<string> apps = new List<string>();
                if (module.GetConfig().GetValueOrDefault("applications", null) != null) {
                    apps = Serializer.Deserialize<List<string>>(module.GetConfig().GetValue("applications").ToString());
                }
                if (apps.Contains(arguments[1])) {
                    apps.Remove(arguments[1]);
                    module.GetConfig().Put("app-" + arguments[1], null);
                    module.GetConfig().Put("applications", Serializer.Serialize(apps));
                    Bot.WriteLine("Application has been unlinked.");
                    module.SaveConfig();
                } else {
                    Bot.WriteLine("Application not found.");
                }
            } else {
                Bot.WriteLine("Invalid usage.");
            }
        }
    }
}
