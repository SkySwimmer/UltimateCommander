using System;
using CMDR;
using SubObuscate;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Discord.WebSocket;
using System.IO;
using Discord;
using Discord.Rest;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace link_r
{
    public static class Base64Url
    {
        public static string Encode(byte[] arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = Convert.ToBase64String(arg);
            return s
                .Replace("=", "")
                .Replace("/", "_")
                .Replace("+", "-");
        }

        public static string ToBase64(string arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = arg
                    .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                    .Replace("_", "/")
                    .Replace("-", "+");

            return s;
        }

        public static byte[] Decode(string arg)
        {
            var decrypted = ToBase64(arg);

            return Convert.FromBase64String(decrypted);
        }
    }

    public class Module : BotModule
    {
        public override string id => "Link_r";

        public override string moduledesctiption => "Roblox verification bot";

        public Stream linkInput;
        public Stream linkOuput;

        private List<IntentResult> output = new List<IntentResult>();
        private RSAParameters rsaKeyInfo;
        private RSA rsa;

        public byte[] GenerateLinkJWT(ulong gid, string appname)
        {
            Thread.Sleep(3000);
            long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

            string header = Base64Url.Encode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, string>() {
                ["alg"] = "RS512",
                ["typ"] = "JWT"
            })));

            string ID = Guid.NewGuid().ToString();
            if (gid != 0) {
                SocketGuild guild = GetBot().client.GetGuild(gid);
                Server srv = GetBot().GetServerFromSocketGuild(guild);
                Server.ModuleConfig conf = srv.GetModuleConfig(this);
                while (conf.Get("app-" + ID) != null) {
                    ID = Guid.NewGuid().ToString();
                }
            } else {
                while (GetConfig().GetValueOrDefault("app-" + ID, null) != null) {
                    ID = Guid.NewGuid().ToString();
                }
            }
            string payload = Base64Url.Encode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Dictionary<string, object>() {
                ["iat"] = DateTimeOffset.Now.ToUnixTimeSeconds(),
                ["iss"] = "LinkR-" + GetBot().client.CurrentUser.Id,
                ["sub"] = "" + gid,
                ["jti"] = ID,
                ["nbf"] = DateTimeOffset.Now.ToUnixTimeSeconds() + 30,
                ["appname"] = appname,
                ["domain"] = gid
            })));

            string data = header + "." + payload;
            byte[] sig = rsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);
            string JWT = data + "." + Base64Url.Encode(sig);
            if (gid != 0) {
                SocketGuild guild = GetBot().client.GetGuild(gid);
                Server srv = GetBot().GetServerFromSocketGuild(guild);
                Server.ModuleConfig conf = srv.GetModuleConfig(this);
                conf.Set("app-" + ID, JWT);

                List<string> apps = new List<string>();
                if (conf.Get("applications") != null) {
                    apps = Serializer.Deserialize<List<string>>(conf.Get("applications").ToString());
                }
                apps.Add(ID);
                conf.Set("applications", Serializer.Serialize(apps));
            } else {
                GetConfig().Put("app-" + ID, JWT);

                List<string> apps = new List<string>();
                if (GetConfig().GetValueOrDefault("applications", null) != null) {
                    apps = Serializer.Deserialize<List<string>>(GetConfig().GetValue("applications").ToString());
                }
                apps.Add(ID);
                GetConfig().Put("applications", Serializer.Serialize(apps));

                SaveConfig();
            }
            return Encoding.UTF8.GetBytes(JWT);
        }

        public override void Init(Bot bot)
        {
            if (GetConfig().GetValue("website") == null) {
                GetConfig().Put("website", "https://aerialworks.ddns.net/linkr");
                SaveConfig();
            }

            if (GetConfig().GetValue("publickey") == null || GetConfig().GetValue("privatekey") == null) {
                rsa = RSA.Create();
                rsaKeyInfo = rsa.ExportParameters(false);
                byte[] pubKey = rsa.ExportRSAPublicKey();
                byte[] privKey = rsa.ExportRSAPrivateKey();
                GetConfig().Put("publickey", Convert.ToBase64String(pubKey));
                GetConfig().Put("privatekey", Convert.ToBase64String(privKey));
                SaveConfig();
            } else {
                rsa = RSA.Create();
                int dummy;
                rsa.ImportRSAPublicKey(Convert.FromBase64String(GetConfig().GetValue("publickey").ToString()), out dummy);
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(GetConfig().GetValue("privatekey").ToString()), out dummy);
                rsaKeyInfo = rsa.ExportParameters(false);
            }

            IntentPool.RegisterIntent(new LoginIntent());
            IntentPool.RegisterIntent(new LinkIntent());
            IntentPool.RegisterIntent(new VerificationBackend());

            if (OperatingSystem.IsWindows()) {
                linkInput = NativeInterace.GetWindowsNativeInterface().CreatePipeFile("link_r_backendlink.pipein");
                linkOuput = NativeInterace.GetWindowsNativeInterface().CreatePipeFile("link_r_backendlink.pipeout");
            } else {
                if (File.Exists("link_r_backendlink.pipein")) {
                    File.Delete("link_r_backendlink.pipein");
                }
                Process proc = Process.Start("mkfifo", "link_r_backendlink.pipein");
                proc.WaitForExit();
                if (proc.ExitCode != 0) {
                    throw new Exception("Backend statup failure");
                }
                proc = Process.Start("chmod", "ug=rwx link_r_backendlink.pipein");
                proc.WaitForExit();
                if (proc.ExitCode != 0) {
                    throw new Exception("Backend statup failure");
                }
                if (File.Exists("link_r_backendlink.pipeout")) {
                    File.Delete("link_r_backendlink.pipeout");
                }
                proc = Process.Start("mkfifo", "link_r_backendlink.pipeout");
                proc.WaitForExit();
                if (proc.ExitCode != 0) {
                    throw new Exception("Backend statup failure");
                }
                proc = Process.Start("chmod", "ug=rwx link_r_backendlink.pipeout");
                proc.WaitForExit();
                if (proc.ExitCode != 0) {
                    throw new Exception("Backend statup failure");
                }
                linkInput = new FileStream("link_r_backendlink.pipein", FileMode.Open, FileAccess.ReadWrite);
                linkOuput = new FileStream("link_r_backendlink.pipeout", FileMode.Open, FileAccess.ReadWrite);
            }
            new Thread(() => {
                while (true) {
                    byte[] prefix = new byte[4];
                    linkInput.Read(prefix);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(prefix);
                    
                    try {
                        byte[] packet = new byte[BitConverter.ToInt32(prefix)];
                        linkInput.Read(packet);

                        MemoryStream packetStream = new MemoryStream(packet);
                        byte method = (byte)packetStream.ReadByte();
                        if (method == 0) {
                            byte[] buffer = new byte[8];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            ulong domain = BitConverter.ToUInt64(buffer);
                            
                            buffer = new byte[8];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            ulong subdomain = BitConverter.ToUInt64(buffer);
                            
                            buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            int l = BitConverter.ToInt32(buffer);
                            byte[] data = new byte[l];
                            packetStream.Read(data);
                            string address = Encoding.UTF8.GetString(data);
                            
                            buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            l = BitConverter.ToInt32(buffer);
                            data = new byte[l];
                            packetStream.Read(data);
                            string token = Encoding.UTF8.GetString(data);
                            
                            buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            l = BitConverter.ToInt32(buffer);
                            data = new byte[l];
                            packetStream.Read(data);
                            string parameters = Encoding.UTF8.GetString(data);
                            
                            buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            l = BitConverter.ToInt32(buffer);
                            data = new byte[l];
                            packetStream.Read(data);
                            byte[] payload = data;

                            packetStream.Close();
                            output.Add(SubsystemAccessor.Access(domain, subdomain, address, token, parameters, payload));
                        } else if (method == 1) {
                            byte[] buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            int l = BitConverter.ToInt32(buffer);
                            byte[] data = new byte[l];
                            packetStream.Read(data);
                            string token = Encoding.UTF8.GetString(data);

                            buffer = new byte[8];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            ulong subdomain = BitConverter.ToUInt64(buffer);

                            buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            l = BitConverter.ToInt32(buffer);
                            data = new byte[l];
                            packetStream.Read(data);
                            string intent = Encoding.UTF8.GetString(data);

                            buffer = new byte[4];
                            packetStream.Read(buffer);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(buffer);
                            l = BitConverter.ToInt32(buffer);
                            data = new byte[l];
                            packetStream.Read(data);
                            string responseID = Encoding.UTF8.GetString(data);
                            
                            if (intent == "Login" || intent == "Link") {
                                output.Add(IntentResult.FailureFrom(responseID, 3));
                            } else {
                                try {
                                    string header = token.Split(".")[0];
                                    string payload = token.Split(".")[1];
                                    string signature = token.Split(".")[2];
                                    
                                    string payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(payload));
                                    string headerJson = Encoding.UTF8.GetString(Base64Url.Decode(header));
                                    byte[] sig = Base64Url.Decode(signature);

                                    if (rsa.VerifyData(Encoding.UTF8.GetBytes(header + "." + payload), sig, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1)) {
                                        RunnerResult res = new RunnerResult();
                                        
                                        bool process = true;
                                        Dictionary<string, object> info = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson);
                                        if (((long)info["nbf"]) > DateTimeOffset.Now.ToUnixTimeSeconds()) {
                                            output.Add(IntentResult.FailureFrom(responseID, 3));
                                            process = false;
                                            continue;
                                        }

                                        if (process) {
                                            ulong gid = ulong.Parse(info["domain"].ToString());
                                            if (gid == 0) {
                                                if (GetConfig().GetValueOrDefault("app-" + info["jti"], null) != null) {
                                                    IntentRunner runner = IntentRunner.Spin(gid, subdomain, intent);

                                                    res.domain = runner.GetDomain();
                                                    res.subDomain = runner.GetSubdomain();
                                                    res.address = runner.GetSubsystemAddress();
                                                    res.intent = runner.GetIntentToken();

                                                    output.Add(IntentResult.FromObject(responseID, res));
                                                } else {
                                                    output.Add(IntentResult.FailureFrom(responseID, 1));
                                                }
                                            } else {
                                                SocketGuild guild = GetBot().client.GetGuild(gid);
                                                Server srv = GetBot().GetServerFromSocketGuild(guild);
                                                Server.ModuleConfig conf = srv.GetModuleConfig(this);
                                                if (conf.Get("app-" + info["jti"]) != null) {
                                                    IntentRunner runner = IntentRunner.Spin(gid, subdomain, intent);
                                                    
                                                    res.domain = runner.GetDomain();
                                                    res.subDomain = runner.GetSubdomain();
                                                    res.address = runner.GetSubsystemAddress();
                                                    res.intent = runner.GetIntentToken();

                                                    output.Add(IntentResult.FromObject(responseID, res));
                                                } else {
                                                    output.Add(IntentResult.FailureFrom(responseID, 1));
                                                }
                                            }
                                        }
                                    } else {
                                        output.Add(IntentResult.FailureFrom(responseID, 1));
                                    }
                                } catch {
                                    output.Add(IntentResult.FailureFrom(responseID, 1));
                                }
                            }
                        }
                    } catch {
                    }
                }
            }).Start();
            new Thread(() => {
                while (true) {
                    while (output.Count == 0) {
                        Thread.Sleep(1);
                    }
                    List<IntentResult> results = new List<IntentResult>(output);
                    foreach (IntentResult res in results) {
                        MemoryStream packetStream = new MemoryStream();
                        packetStream.WriteByte(res.GetResult());

                        byte[] address = Encoding.UTF8.GetBytes(res.GetSubsystemAddress());
                        byte[] lBuf = BitConverter.GetBytes(address.Length);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(lBuf);
                        packetStream.Write(lBuf);
                        packetStream.Write(address);

                        if (res.GetResult() == 0) {
                            byte[] payload = res.GetPayload();
                            lBuf = BitConverter.GetBytes(payload.Length);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(lBuf);
                            packetStream.Write(lBuf);
                            packetStream.Write(payload);
                        }
                        packetStream.Close();

                        byte[] packet = packetStream.ToArray();
                        lBuf = BitConverter.GetBytes(packet.Length);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(lBuf);
                        
                        linkOuput.Write(lBuf);
                        linkOuput.Write(packet);
                        linkOuput.Flush();

                        output.Remove(res);
                    }
                }
            }).Start();
        }

        public class RunnerResult {
            public string address;
            public string intent;
            public ulong domain;
            public ulong subDomain;
        }

        internal Dictionary<Server, ConfigDictionary<string, object>> serverMemory = new Dictionary<Server, ConfigDictionary<string, object>>();

        void JoinedNewServer(Server server, SocketGuild guild) {
            server.GetModuleConfig(this).Set("PreparationCompleted", true);
            server.GetModuleConfig(this).Set("SetupCompleted", false);

            startServerHandlerThread(server, guild);
        }

        public delegate void GuildUserEvent(SocketGuildUser member);
        private void startServerHandlerThread(Server server, SocketGuild guild)
        {
            Server.ModuleConfig conf = server.GetModuleConfig(this);
            List<ulong> members;
            bool changed = false;
            if (conf.Get("Members") == null) {
                members = new List<ulong>();
                changed = true;
            } else
                members = Serializer.Deserialize<List<ulong>>(conf.Get("Members").ToString());
            foreach (ulong id in new List<ulong>(members)) {
                if (guild.GetUser(id) == null) {
                    changed = true;
                    members.Remove(id);
                    conf.Set("user-" + id, null);
                    conf.Set("user-verification-message-" + id, (ulong)0);
                }
            }
            if (changed)
                conf.Set("Members", Serializer.Serialize(members));

            ConfigDictionary<string, object> mem = serverMemory[server];
            mem.Put("MemberSyncHandler", new GuildUserEvent((member) => {
                if (!member.IsBot) {
                    if ((bool)conf.GetOrDefault("SetupCompleted", false)) {
                        if (!members.Contains(member.Id)) {
                            verifyMember(server, guild, member, conf);
                        }
                    }
                }
            }));
        }

        void verifyMember(Server server, SocketGuild guild, SocketGuildUser member, Server.ModuleConfig conf) {
            bool loaded = false;
            if ((ulong)GetConfig().GetValueOrDefault("defaultaccount-" + member.Id, (ulong)0) != 0) {
                if (linkUser(server, guild, member, conf, (ulong)GetConfig().GetValueOrDefault("defaultaccount-" + member.Id, 0), true, false)) {
                    loaded = true;
                }
            }

            if (!loaded) {
                SocketTextChannel channel = guild.GetTextChannel((ulong)conf.Get("verification.channel"));
                if (channel == null)
                    channel = guild.DefaultChannel;
                
                ulong oID = ulong.Parse(conf.GetOrDefault("user-verification-message-" + member.Id, (ulong)0).ToString());
                if (oID != 0) {
                    IMessage oMsg = channel.GetMessageAsync(oID).GetAwaiter().GetResult();
                    if (oMsg != null) {
                        var r = oMsg.Reactions.FirstOrDefault(t => t.Key.Name == new Emoji("\uD83D\uDC4D").Name, new KeyValuePair<IEmote, ReactionMetadata>(null, new ReactionMetadata()));
                        if (r.Key != null) {
                            var e = oMsg.GetReactionUsersAsync(new Emoji("\uD83D\uDC4D"), r.Value.ReactionCount).GetAsyncEnumerator();
                            while (true) {
                                if (e.Current != null) {
                                    foreach (IUser user in e.Current) {
                                        if (user.Id == member.Id) {
                                            handleStart(guild, oMsg, new Emoji("\uD83D\uDC4D"), channel, member);
                                        }
                                    }
                                }
                                if (!e.MoveNextAsync().GetAwaiter().GetResult())
                                    break;
                            }
                        }

                        return;
                    }
                }

                RestUserMessage msg = channel.SendMessageAsync(conf.Get("verification.channel.message.template").ToString()
                    .Replace("%mention%", "<@" + member.Id + ">") + "\n\n**React with :thumbsup: to begin the verification process...**").GetAwaiter().GetResult();
                msg.AddReactionAsync(new Emoji("\uD83D\uDC4D")).GetAwaiter().GetResult();
                conf.Set("user-verification-message-" + member.Id, msg.Id);
            }
        }

        public class UserInfo {
            public string description;
            public string created;
            public bool isBanned;
            public string externalAppDisplayName;
            public ulong id;
            public string name;
            public string displayName;
        }

        public class LinkedUser {
            public ulong discordID = 0;
            public ulong robloxUserId = 0;

            public string username = "";
            public string displayName = "";
        }

        public bool linkUser(Server server, SocketGuild guild, SocketGuildUser member, Server.ModuleConfig conf, ulong account, bool wasLoggedIn, bool setAsMainAccount) {
            try {
                string json = new HttpClient().GetStringAsync("https://users.roblox.com/v1/users/" + account).GetAwaiter().GetResult();
                UserInfo info = JsonConvert.DeserializeObject<UserInfo>(json);
                if (info.isBanned)
                    return false;

                SocketTextChannel tChannel = guild.GetTextChannel((ulong)conf.Get("verification.postver.channel"));
                if (tChannel == null)
                    tChannel = guild.DefaultChannel;
                if (!wasLoggedIn) {
                    SocketTextChannel channel = guild.GetTextChannel((ulong)conf.Get("verification.channel"));
                    if (channel == null)
                        channel = guild.DefaultChannel;
                    channel.SendMessageAsync("Verification completed <@" + member.Id + ">, you can now proceed to <#" + tChannel.Id + ">.").GetAwaiter().GetResult();
                }

                LinkedUser user = new LinkedUser();
                user.discordID = member.Id;
                user.robloxUserId = account;
                user.username = info.name;
                user.displayName = info.displayName;
                if (user.displayName == null)
                    user.displayName = info.name;
                conf.Set("user-" + member.Id, Serializer.Serialize(user));
                conf.Set("user-verification-message-" + member.Id, 0);

                List<ulong> accounts = new List<ulong>();
                if (GetConfig().ContainsKey("accounts-" + member.Id)) {
                    accounts = Serializer.Deserialize<List<ulong>>(GetConfig().GetValue("accounts-" + member.Id).ToString());
                }
                if (!accounts.Contains(account)) {
                    accounts.Add(account);
                    GetConfig().Put("accounts-" + member.Id, Serializer.Serialize(accounts));
                    SaveConfig();
                }
                if (setAsMainAccount) {
                    GetConfig().Put("defaultaccount-" + member.Id, account);
                    SaveConfig();
                }
                
                List<ulong> members = Serializer.Deserialize<List<ulong>>(conf.Get("Members").ToString());
                members.Add(member.Id);
                conf.Set("Members", Serializer.Serialize(members));

                if ((bool)conf.Get("verification.nicknames.overridenickname")) {
                    try {
                        if ((bool)conf.Get("verification.nicknames.usedisplayname")) {
                            member.ModifyAsync(t => t.Nickname = user.displayName).GetAwaiter().GetResult();
                        } else {
                            member.ModifyAsync(t => t.Nickname = user.username).GetAwaiter().GetResult();
                        }
                    } catch {
                    }
                }

                member.AddRoleAsync((ulong)conf.Get("verification.memberrole")).GetAwaiter().GetResult();
                tChannel.SendMessageAsync(conf.Get("verification.postver.message.template").ToString().Replace("%mention%", "<@" + member.Id + ">")).GetAwaiter().GetResult();
                return true;
            } catch {
                return false;
            }
        }

        void LeftServer(Server server) {
            Dictionary<string, VerificationInfo> codes;
            while (true) {
                try {
                    codes = new Dictionary<string, VerificationInfo>(VerifyCodes);
                    break;
                } catch {
                }
            }

            foreach (string key in codes.Keys) {
                VerificationInfo info = codes[key];
                if (info.guild.Id == server.id) {
                    VerifyCodes.Remove(key);
                }
            }
        }

        public override void PostInit(Bot bot)
        {
            bot.client.JoinedGuild += (guild) => {
                Server s = bot.GetServerFromSocketGuild(guild);
                while (s == null) {
                    s = bot.GetServerFromSocketGuild(guild);
                    Thread.Sleep(1);
                }

                JoinedNewServer(s, guild);
                serverMemory[s] = new ConfigDictionary<string, object>();
                return Task.CompletedTask;
            };
            bot.client.LeftGuild += (guild) => {
                foreach (Server srv in new List<Server>(serverMemory.Keys)) {
                    if (srv.id == guild.Id) {
                        LeftServer(srv);
                        serverMemory.Remove(srv);
                        break;
                    }
                }

                return Task.CompletedTask;
            };
            
            foreach (Server srv in GetServers()) {
                serverMemory[srv] = new ConfigDictionary<string, object>();
                if (!(bool)srv.GetModuleConfig(this).GetOrDefault("PreparationCompleted", false)) {
                    JoinedNewServer(srv, bot.client.GetGuild(srv.id));
                } else
                    startServerHandlerThread(srv, bot.client.GetGuild(srv.id));
            }

            bot.client.UserJoined += (member) => {
                SocketGuild guild = member.Guild;
                if (serverMemory[bot.GetServerFromSocketGuild(guild)] != null) {
                    ((GuildUserEvent)serverMemory[bot.GetServerFromSocketGuild(guild)]["MemberSyncHandler"])(member);
                }
                return Task.CompletedTask;
            };
            bot.client.UserLeft += (guild, member) => {
                if (serverMemory[bot.GetServerFromSocketGuild(guild)] != null) {
                    List<ulong> members = Serializer.Deserialize<List<ulong>>(bot.GetServerFromSocketGuild(guild).GetModuleConfig(this).Get("Members").ToString());
                    if (members.Contains(member.Id)) {
                        members.Remove(member.Id);
                        bot.GetServerFromSocketGuild(guild).GetModuleConfig(this).Set("Members", Serializer.Serialize(members));
                        bot.GetServerFromSocketGuild(guild).GetModuleConfig(this).Set("user-" + member.Id, null);
                        bot.GetServerFromSocketGuild(guild).GetModuleConfig(this).Set("user-verification-message-" + member.Id, (ulong)0);
                    }
                }

                Dictionary<string, VerificationInfo> codes;
                while (true) {
                    try {
                        codes = new Dictionary<string, VerificationInfo>(VerifyCodes);
                        break;
                    } catch {
                    }
                }

                foreach (string key in codes.Keys) {
                    VerificationInfo info = codes[key];
                    if (info.guild.Id == guild.Id && info.member.Id == member.Id) {
                        VerifyCodes.Remove(key);
                    }
                }

                return Task.CompletedTask;
            };
            bot.client.ReactionAdded += (message, channel, reaction) => {
                if (channel.Value != null && channel.Value is SocketTextChannel) {
                    SocketTextChannel ch = (SocketTextChannel)channel.Value;
                    SocketGuildUser member = (SocketGuildUser)reaction.User;
                    IMessage msg = ch.GetMessageAsync(message.Id).GetAwaiter().GetResult();
                    SocketGuild guild = ch.Guild;

                    Server server = GetBot().GetServerFromSocketGuild(guild);
                    Server.ModuleConfig conf = server.GetModuleConfig(this);
                    if ((ulong)server.GetModuleConfig(this).GetOrDefault("user-verification-message-" + member.Id, (ulong)0) == message.Id) {
                        handleStart(guild, msg, reaction.Emote, ch, member);
                    }
                }
                return Task.CompletedTask;
            };

            foreach (Server srv in GetServers()) {
                var en = bot.client.GetGuild(srv.id).GetUsersAsync().GetAsyncEnumerator();
                while (true) {
                    if (en.Current != null) {
                        foreach (IGuildUser user in en.Current) {
                            if (user is SocketGuildUser) {
                                ((GuildUserEvent)serverMemory[srv]["MemberSyncHandler"])((SocketGuildUser)user);
                            }
                        }
                    }
                    if (!en.MoveNextAsync().GetAwaiter().GetResult())
                        break;
                }
            }
        }

        public Dictionary<string, VerificationInfo> VerifyCodes = new Dictionary<string, VerificationInfo>();
        public class VerificationInfo {
            public SocketGuild guild;
            public SocketGuildUser member;
            public Server server;
            public Server.ModuleConfig conf;
        }

        void handleStart(SocketGuild guild, IMessage msg, IEmote emote, SocketTextChannel ch, SocketGuildUser member) {
            Server server = GetBot().GetServerFromSocketGuild(guild);
            Server.ModuleConfig conf = server.GetModuleConfig(this);

            msg.RemoveReactionAsync(emote, member).GetAwaiter().GetResult();
            try {
                IDMChannel dm = member.CreateDMChannelAsync().GetAwaiter().GetResult();
                IntentRunner runner = IntentRunner.Spin(guild.Id, member.Id, "Login", new LoginIntent.StartParameters() {
                    guild = guild.Id,
                    discordUserId = member.Id
                });

                Dictionary<string, VerificationInfo> codes;
                while (true) {
                    try {
                        codes = VerifyCodes = new Dictionary<string, VerificationInfo>(VerifyCodes);
                        break;
                    } catch {
                    }
                }
                foreach (string c in codes.Keys) {
                    VerificationInfo info = codes[c];
                    if (info.member.Id == member.Id) {
                        VerifyCodes.Remove(c);
                    }
                }
    
                string code = null;
                while (true) {
                    try {
                        code = Guid.NewGuid().ToString();
                        if (!VerifyCodes.ContainsKey(code)) {
                            VerifyCodes[code] = new VerificationInfo() {
                                guild = guild,
                                member = member,
                                server = server,
                                conf = conf
                            };
                            break;
                        }
                    } catch {
                    }
                }

                Thread cT = new Thread(() => {
                    int timeLeft = 15 * 60;
                    while (true) {
                        if (timeLeft == 0) {
                            VerifyCodes.Remove(code);
                            break;
                        }
                        timeLeft -= 1;
                        Thread.Sleep(1000);
                    }
                    VerifyCodes.Remove(code);
                });
                cT.Name = "CodeThread: " + code;
                cT.Start();

                dm.SendMessageAsync(conf.Get("verification.message.template").ToString()
                    .Replace("%verificationcode%", code)
                    .Replace("%mention%", "<@" + member.Id + ">")
                    .Replace("%url%", Bot.GetBot().GetModule("Link_r").GetConfig().GetValueOrDefault("website", "https://aerialworks.ddns.net/linkr").ToString() + "/%urlsuffix%")
                    .Replace("%urlsuffix%","verify?code=" + code
                            + "&user=" + member.Id
                            + "&guild=" + guild.Id
                            + "&subsystemaddress=" + runner.GetSubsystemAddress()
                            + "&intenttoken=" + runner.GetIntentToken()
                            + "&bot=" + Bot.GetBot().client.CurrentUser.Id)
                    + "\n\n**Note:**\n***This message was sent from '" + guild.Name + "'***").GetAwaiter().GetResult();
            } catch {
                ch.SendMessageAsync("An error occured verifying <@" + member.Id + ">, please report this to an admin.").GetAwaiter().GetResult();
            }
        }

        public override void PreInit(Bot bot)
        {
            
        }

        public override void RegisterCommands(Bot bot)
        {
            RegisterCommand(new SetupCommand(this));
            RegisterCommand(new CancelSetupCommand(this));
            RegisterCommand(new ChangeOptionCommand(this));
            RegisterCommand(new LoginCommand(this));
            RegisterCommand(new UpdateNicknameCommand(this));
            RegisterCommand(new GetUserInfoCommand(this));
            RegisterCommand(new LinkAppCommand(this));
        }
    }
}
