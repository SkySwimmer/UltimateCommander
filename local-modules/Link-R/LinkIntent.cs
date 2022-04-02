using System.Collections.Generic;
using System.Threading;
using CMDR;
using SubObuscate;

namespace link_r {

    public class LinkIntent : Intent
    {
        public string IntentName => "Link";
        private Dictionary<ulong, int> rateLimitMemory = new Dictionary<ulong, int>();

        public IntentResult Execute(IntentParameters parameters)
        {
            if (parameters.GetDomain() != 0) {
                ulong gid = parameters.GetDomain();
                if (!rateLimitMemory.ContainsKey(gid)) {
                    rateLimitMemory[gid] = 1;
                    Thread rateLimitThread = new Thread(() => {
                        int timeLeft = 60 * 60;
                        while (rateLimitMemory.ContainsKey(gid)) {
                            if (timeLeft == 0) {
                                rateLimitMemory.Remove(gid);
                                break;
                            } else
                                timeLeft--;
                            Thread.Sleep(1000);
                        }
                    });
                    rateLimitThread.Name = "RateLimitThread: " + gid;
                    rateLimitThread.Start();
                } else if (rateLimitMemory[gid] >= 30) {
                     return IntentResult.FromObject(parameters, "rate-limit-error");
                } else
                    rateLimitMemory[gid] += 1;
            }
            return IntentResult.FromRaw(parameters, ((Module)Bot.GetBot().GetModule("Link_r")).GenerateLinkJWT(parameters.GetDomain(), parameters.GetParameters<string>()));
        }

        public Intent Instantiate()
        {
            return new LinkIntent();
        }
    }

}