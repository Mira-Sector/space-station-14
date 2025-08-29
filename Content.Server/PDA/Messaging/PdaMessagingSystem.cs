using Content.Shared.PDA.Messaging;

namespace Content.Server.PDA.Messaging;

public sealed partial class PdaMessagingSystem : SharedPdaMessagingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeServer();
    }
}
