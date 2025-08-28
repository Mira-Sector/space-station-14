using System.Linq;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class ChatCartridgeSystem : SharedChatCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    protected override void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2))
            return;

        var recipients = PdaMessaging.GetClientRecipients((ent.Owner, ent.Comp2));
        Dictionary<IPdaChatRecipient, BasePdaChatMessage[]> messages = new(recipients.Count());
        foreach (var recipient in recipients)
            messages[recipient] = PdaMessaging.GetHistory(ent.Owner, recipient).ToArray();

        var state = new ChatUiState(ent.Comp2.Profile, messages);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
