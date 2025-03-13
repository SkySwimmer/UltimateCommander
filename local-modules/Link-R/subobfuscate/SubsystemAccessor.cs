namespace SubObuscate {

    public static class SubsystemAccessor {
        public static IntentResult Access(ulong domain, ulong subdomain, string address, string intentToken, string parameters, byte[] payload) {
            IntentParameters ipr;
            try {
                ipr = IntentParameters.CreateParameters(domain, subdomain, address, intentToken, parameters, payload);
            } catch {
                return IntentResult.FailureFrom(address, 4);
            }
            IntentRunner runner = IntentRunner.subsystems.Find(t => {
                return t.GetDomain() == domain && t.GetSubdomain() == subdomain && t.GetSubsystemAddress() == address;
            });
            if (runner == null)
                return IntentResult.FailureFrom(ipr, 4);
            if (runner.GetIntentToken() != intentToken) {
                runner.Shutdown = true;
                IntentRunner.subsystems.Remove(runner);
                return IntentResult.FailureFrom(ipr, 2);
            }

            try {
                var res = runner.Intent.Execute(ipr);
                runner.Shutdown = true;
                IntentRunner.subsystems.Remove(runner);
                return res;
            } catch {
                runner.Shutdown = true;
                IntentRunner.subsystems.Remove(runner);
                return IntentResult.FailureFrom(ipr, 1);
            }
        }
    }

}