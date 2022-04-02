using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SubObuscate {
    public class IntentRunner {
        private IntentRunner(){}
        
        private static Random rnd = new Random();
        internal static List<IntentRunner> subsystems = new List<IntentRunner>();

        private Dictionary<string, string> IntentNames = new Dictionary<string, string>();

        private ulong Domain;
        private ulong Subdomain;
        private string Address;
        private string IntentToken;
        internal Intent Intent;
        internal bool Shutdown = false;

        public ulong GetDomain() {
            return Domain;
        }

        public ulong GetSubdomain() {
            return Subdomain;
        }

        public string GetSubsystemAddress() {
            return Address;
        }

        public string GetIntentToken() {
            return IntentToken;
        }

        private static IntentRunner GetIntentRunner(String address) {
            List<IntentRunner> runners = new List<IntentRunner>(subsystems);
            foreach (IntentRunner runner in runners) {
                if (runner.Address == address)
                    return runner;
            }
            return null;
        }

        public static IntentRunner Spin(ulong domain, ulong subdomain, string intent, object properties = null) {
            IntentRunner runner = new IntentRunner();
            runner.Domain = domain;
            runner.Subdomain = subdomain;
            runner.Intent = IntentPool.GetIntentType(intent);
            if (runner.Intent == null)
                throw new ArgumentException("Intent not found: " + intent);
            runner.Intent = runner.Intent.Instantiate();
            if (properties != null)
                runner.Intent.LoadProperties(properties);

            IntentRunner firstDomainRunner = new List<IntentRunner>(subsystems).Find(t => t.Domain == domain);
            if (firstDomainRunner != null) {
                runner.IntentNames = firstDomainRunner.IntentNames;
            }
            Thread intentThread = new Thread(() => {
                int timeLeft = 15 * 60;
                while (!runner.Shutdown) {
                    if (timeLeft == 0) {
                        runner.Shutdown = true;
                        subsystems.Remove(runner);
                        break;
                    }
                    timeLeft -= 1;
                    Thread.Sleep(1000);
                }
            });
            intentThread.Name = "IntentThread: " + runner.Intent.GetType().Name;
            intentThread.Start();
            IntentRunner rn = new List<IntentRunner>(subsystems).Find(t => t.Domain == domain && t.Subdomain == subdomain && t.Intent.IntentName == intent);
            if (rn != null) {
                rn.Shutdown = true;
                subsystems.Remove(rn);
            }

            if (firstDomainRunner == null) {
                foreach (string intentN in IntentPool.GetIntentNames()) {
                    string token = "" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z');
                    while (runner.IntentNames.ContainsValue(token)) {
                        token = "" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z');
                    }
                    runner.IntentNames[intentN] = token;
                }
            }
            while (true) {
                string addr = "" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')+ (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')
                                            + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z')+ (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z');
                
                IntentRunner fdr = firstDomainRunner;
                if (fdr == null)
                    fdr = runner;
                if (GetIntentRunner(addr) == null) {
                    runner.Address = addr;
                    while (!fdr.IntentNames.ContainsKey(intent))
                        Thread.Sleep(1);
                    runner.IntentToken = fdr.IntentNames[intent];
                    break;
                }
            }
            subsystems.Add(runner);
            if (firstDomainRunner == null) {
                Thread obfusThread = new Thread(() => {
                    int timeLeft = 5 * 60;
                    IntentRunner firstDomainRunner = new List<IntentRunner>(subsystems).Find(t => t.Domain == domain);
                    while (firstDomainRunner != null) {
                        if (timeLeft == 0) {
                            firstDomainRunner.IntentNames.Clear();
                            foreach (string intent in IntentPool.GetIntentNames()) {
                                string token = "" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z');
                                while (firstDomainRunner.IntentNames.ContainsValue(token)) {
                                    token = "" + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z') + (char)rnd.Next('0','9') + (char)rnd.Next('0','9') + (char)rnd.Next('A','Z') + (char)rnd.Next('A','Z');
                                }
                                firstDomainRunner.IntentNames[intent] = token;
                            }
                            timeLeft = 5 * 60;
                        }
                        timeLeft -= 1;
                        Thread.Sleep(1000);
                    }
                });
                obfusThread.Name = "Intent Domain Thread: " + runner.Domain;
                obfusThread.Start();
            }
            return runner;
        }
    }
}
