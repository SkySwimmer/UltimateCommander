namespace SubObuscate {

    public interface Intent {
        public string IntentName {get;}
        public Intent Instantiate();
        public void LoadProperties(object properties) {}
        
        public IntentResult Execute(IntentParameters parameters);
    }

}
