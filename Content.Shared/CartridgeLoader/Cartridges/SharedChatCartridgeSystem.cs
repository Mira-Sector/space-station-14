using Content.Shared.PDA.Messaging;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Messages;
using System.Diagnostics.CodeAnalysis;

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

        SubscribeLocalEvent<ChatCartridgeComponent, PdaMessageClientReceiveRecipientsEvent>(OnReceiveRecipients, after: [typeof(SharedPdaMessagingSystem)]);

        SubscribeLocalEvent<ChatCartridgeComponent, PdaMessageClientServerConnectedEvent>(OnServerConnected, after: [typeof(SharedPdaMessagingSystem)]);
        SubscribeLocalEvent<ChatCartridgeComponent, PdaMessageClientServerDisconnectedEvent>(OnServerDisconnected, after: [typeof(SharedPdaMessagingSystem)]);
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
        PlayNotification(ent, args.Message);
    }

    private void OnReceiveRecipients(Entity<ChatCartridgeComponent> ent, ref PdaMessageClientReceiveRecipientsEvent args)
    {
        UpdateUi(ent);
    }

    private void OnServerConnected(Entity<ChatCartridgeComponent> ent, ref PdaMessageClientServerConnectedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnServerDisconnected(Entity<ChatCartridgeComponent> ent, ref PdaMessageClientServerDisconnectedEvent args)
    {
        if (!args.Transferred)
            UpdateUi(ent);
    }

    protected void PlayNotification(Entity<ChatCartridgeComponent> ent, BasePdaChatMessage message)
    {
        if (!TryGetLoader(ent.Owner, out var loader))
            return;

        PlayNotification(ent, loader.Value, message);
    }

    protected virtual void PlayNotification(Entity<ChatCartridgeComponent> ent, EntityUid loader, BasePdaChatMessage message)
    {
    }

    protected void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent)
    {
        if (!TryGetLoader(ent.Owner, out var loader))
            return;

        UpdateUi(ent, loader.Value);
    }

    protected virtual void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
    }

    protected bool TryGetLoader(Entity<CartridgeComponent?> ent, [NotNullWhen(true)] out EntityUid? loader)
    {
        loader = null;

        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        loader = ent.Comp.LoaderUid;
        return loader != null;
    }
}
