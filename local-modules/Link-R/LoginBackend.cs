using CMDR;
using SubObuscate;
using Discord.WebSocket;

namespace link_r {
    public class LoginIntent : Intent
    {
        private StartParameters startParameters;
        public class StartParameters {
            public ulong guild;
            public ulong discordUserId;
        }

        public string IntentName => "Login";

        public Intent Instantiate()
        {
            return new LoginIntent();
        }

        public void LoadProperties(object properties) {
            startParameters = (StartParameters)properties;
        }

        public IntentResult Execute(IntentParameters parameters)
        {
            LoginData data = new LoginData();
            data.guild = startParameters.guild;
            data.discordUserId = startParameters.discordUserId;

            SocketUser user = Bot.GetBot().client.GetUser(data.discordUserId);
            if (user == null)
                return IntentResult.FailureFrom(parameters.GetSubsystemAddress(), 1);
            
            data.username = user.Username;
            string avatar = user.AvatarId;
            if (avatar == null || avatar == "")
                data.avatar = user.GetDefaultAvatarUrl();
            else
                data.avatar = user.GetAvatarUrl(Discord.ImageFormat.Jpeg, 1024);
            return IntentResult.FromObject(parameters, data);
        }

        public class LoginData {
            public ulong guild;
            public string username;
            public ulong discordUserId;
            public string avatar;
        }
    }   
}