using Content.Shared.PDA.Messaging;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;

namespace Content.Shared.CartridgeLoader.Cartridges;

public abstract partial class SharedChatCartridgeSystem : EntitySystem
{
    [Dependency] protected readonly SharedPdaMessagingSystem PdaMessaging = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<ChatCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);

        SubscribeLocalEvent<ChatCartridgeComponent, PdaMessageSendMessageSourceEvent>(OnSentMessageSource, after: [typeof(SharedPdaMessagingSystem)]);
        SubscribeLocalEvent<ChatCartridgeComponent, PdaMessageReplicatedMessageClientEvent>(OnReplicatedMessageClient, after: [typeof(SharedPdaMessagingSystem)]);
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

    private void OnSentMessageSource(Entity<ChatCartridgeComponent> ent, ref PdaMessageSendMessageSourceEvent args)
    {
        UpdateUi(ent);
    }

    private void OnReplicatedMessageClient(Entity<ChatCartridgeComponent> ent, ref PdaMessageReplicatedMessageClientEvent args)
    {
        UpdateUi(ent);
    }

    protected void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent)
    {
        if (!TryComp<CartridgeComponent>(ent.Owner, out var cartridge))
            return;

        if (cartridge.LoaderUid is not { } loader)
            return;

        UpdateUi(ent, loader);
    }

    protected virtual void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
    }
}
