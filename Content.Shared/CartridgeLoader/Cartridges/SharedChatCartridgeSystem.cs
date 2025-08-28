using Content.Shared.PDA.Messaging;
using Content.Shared.PDA.Messaging.Components;

namespace Content.Shared.CartridgeLoader.Cartridges;

public abstract partial class SharedChatCartridgeSystem : EntitySystem
{
    [Dependency] protected readonly SharedPdaMessagingSystem PdaMessaging = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    private void OnUiReady(Entity<ChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader);
    }

    private void OnUiMessage(Entity<ChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not ChatUiMessageEvent message)
            return;

        message.Payload.RunAction(EntityManager);
    }

    protected virtual void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
    }
}
