using System.Linq;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Containers;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class ChatCartridgeSystem : SharedChatCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChatCartridgeComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ChatCartridgeComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnInserted(Entity<ChatCartridgeComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!TryGetLoader(ent.Owner, out var loader))
            return;

        _cartridgeLoader.RegisterBackgroundProgram(loader.Value, ent.Owner);
    }

    private void OnRemoved(Entity<ChatCartridgeComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryGetLoader(ent.Owner, out var loader))
            return;

        _cartridgeLoader.UnregisterBackgroundProgram(loader.Value, ent.Owner);
    }

    protected override void PlayNotification(Entity<ChatCartridgeComponent> ent, EntityUid loader, BasePdaChatMessage message)
    {
        var plural = message.Sender != message.Recipient.GetRecipientMessageable(message);
        var headerWrapper = message.GetHeaderWrapper(plural);
        var header = Loc.GetString(headerWrapper, ("sender", message.Sender.GetNotificationText()), ("receivers", message.Recipient.GetNotificationText()));
        var content = message.GetNotificationText();
        _cartridgeLoader.SendNotification(loader, header, content);
    }

    protected override void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2))
            return;

        var recipients = PdaMessaging.GetClientRecipients((ent.Owner, ent.Comp2));
        Dictionary<BasePdaChatMessageable, BasePdaChatMessage[]> messages = new(recipients.Count());
        foreach (var recipient in recipients)
            messages[recipient] = PdaMessaging.GetHistory(ent.Owner, recipient).ToArray();

        var state = new ChatUiState(ent.Comp2.Profile, messages);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
