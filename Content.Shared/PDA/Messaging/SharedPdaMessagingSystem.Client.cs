using Content.Shared.PDA.Messaging.Components;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<PdaMessagingClientComponent, ComponentInit>(OnClientInit);
    }

    private void OnClientInit(Entity<PdaMessagingClientComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Profile = GetDefaultProfile(ent);
        Dirty(ent);
    }
}
