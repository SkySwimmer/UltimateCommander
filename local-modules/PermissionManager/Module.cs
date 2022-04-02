using System;
using CMDR;

namespace permissionmanager
{
    public class Module : BotModule
    {
        public override string id => "PermissionManager";

        public override string moduledesctiption => "Permission manager module";

        public override void Init(Bot bot)
        {
        }

        public override void PostInit(Bot bot)
        {
        }

        public override void PreInit(Bot bot)
        {
        }

        public override void RegisterCommands(Bot bot)
        {
            RegisterCommand(new PermissionManagerCommand());
        }
    }
}
