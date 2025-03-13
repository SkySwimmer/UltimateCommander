using Newtonsoft.Json;

namespace SubObuscate {

    public class IntentParameters {
        private string address;
        private string intentToken;

        private ulong domain;
        private ulong subDomain;

        private byte[] payload;
        private string parameterData;

        public static IntentParameters CreateParameters(ulong domain, ulong subDomain, string address, string intenttoken, string parameterData, byte[] payload) {
            IntentParameters parameters = new IntentParameters();
            parameters.address = address;
            parameters.intentToken = intenttoken;
            parameters.domain = domain;
            parameters.subDomain = subDomain;
            parameters.payload = payload;
            parameters.parameterData = parameterData;
            return parameters;
        }


        private IntentParameters() {

        }

        public string GetSubsystemAddress() {
            return address;
        }

        public string GetIntentToken() {
            return intentToken;
        }

        public ulong GetDomain() {
            return domain;
        }

        public ulong GetSubdomain() {
            return subDomain;
        }

        public byte[] GetPayload() {
            return payload;
        }

        public T GetParameters<T>() {
            return JsonConvert.DeserializeObject<T>(parameterData);
        }
    }

}