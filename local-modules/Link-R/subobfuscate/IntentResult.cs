using System;
using System.Text;
using Newtonsoft.Json;

namespace SubObuscate {

    public class IntentResult {
        private byte result = 0;
        private string address = "";
        private byte[] payload = new byte[0];
        private object payloadObject = null;

        private IntentResult() {}

        public static IntentResult FromRaw(IntentParameters intent, byte[] payload) {
            IntentResult res = new IntentResult();
            res.result = 0;
            res.address = intent.GetSubsystemAddress();
            res.payload = payload;
            return res;
        }

        public static IntentResult FromRaw(string address, byte[] payload) {
            IntentResult res = new IntentResult();
            res.result = 0;
            res.address = address;
            res.payload = payload;
            return res;
        }

        public static IntentResult FromObject(IntentParameters intent, object payload) {
            IntentResult res = new IntentResult();
            res.result = 0;
            res.address = intent.GetSubsystemAddress();
            res.payloadObject = payload;
            res.payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            return res;
        }

        public static IntentResult FromObject(string address, object payload) {
            IntentResult res = new IntentResult();
            res.result = 0;
            res.address = address;
            res.payloadObject = payload;
            res.payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            return res;
        }

        public static IntentResult FailureFrom(string address, byte error) {
            IntentResult res = new IntentResult();
            res.address = address;
            if (error <= 0 || error > 4)
                throw new ArgumentException("Invalid error code");
            res.result = error;
            return res;
        }

        public static IntentResult FailureFrom(IntentParameters intent, byte error) {
            IntentResult res = new IntentResult();
            res.address = intent.GetSubsystemAddress();
            if (error <= 0 || error > 4)
                throw new ArgumentException("Invalid error code");
            res.result = error;
            return res;
        }

        public byte GetResult() {
            return result;
        }

        public string GetSubsystemAddress() {
            return address;
        }

        public byte[] GetPayload() {
            return payload;
        }

    }

}