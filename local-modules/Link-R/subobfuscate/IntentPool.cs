using System;
using System.Collections.Generic;
using System.Linq;

namespace SubObuscate {
    public static class IntentPool {
        private static List<Intent> IntentRegistry = new List<Intent>();

        public static void RegisterIntent(Intent intent) {
            foreach (Intent i in IntentRegistry) {
                if (i.IntentName == intent.IntentName)
                    return;
            }
            IntentRegistry.Add(intent);
        }

        public static Intent GetIntentType(string name) {
            foreach (Intent i in IntentRegistry) {
                if (i.IntentName == name)
                    return i;
            }
            return null;
        }

        public static string[] GetIntentNames()
        {
            return IntentRegistry.Select(t => t.IntentName).ToArray();
        }
    }
}
