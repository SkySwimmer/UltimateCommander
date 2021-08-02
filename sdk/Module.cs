using System;
using CMDR;

namespace example
{
    public class Module : BotModule
    {
        public override string id => "ExampleModule";

        public override string moduledesctiption => "Default Example Module Template";

        public override void Init(Bot bot)
        {
            Bot.WriteLine("Hello World!");
        }

        public override void PostInit(Bot bot)
        {
            Bot.WriteLine("Hello From PostInit!");
        }

        public override void PreInit(Bot bot)
        {
            Bot.WriteLine("Hello From PreInit!");
        }

        public override void RegisterCommands(Bot bot)
        {
            // Register commands here
        }
    }
}
