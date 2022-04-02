using CMDR;
using SubObuscate;

namespace link_r {

    public class VerificationBackend : Intent
    {
        public string IntentName => "Verify";
        public class VerificationData {
            public ulong robloxUserId = 0;
            public string code = "";
            public bool setAsMainAccount = false;
        }

        public IntentResult Execute(IntentParameters parameters)
        {
            VerificationData data = parameters.GetParameters<VerificationData>();
            Module mod = (Module)Bot.GetBot().GetModule("Link_r");
            if (mod.VerifyCodes.ContainsKey(data.code)) {
                Module.VerificationInfo info = mod.VerifyCodes[data.code];
                mod.VerifyCodes.Remove(data.code);
                if (mod.linkUser(info.server, info.guild, info.member, info.conf, data.robloxUserId, false, data.setAsMainAccount))
                    return IntentResult.FromObject(parameters, "success");
            }

            return IntentResult.FailureFrom(parameters, 3);
        }

        public Intent Instantiate()
        {
            return new VerificationBackend();
        }
    }

}