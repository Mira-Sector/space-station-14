using System.Linq;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA.Messaging.Components;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class ChatCartridgeSystem : SharedChatCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    protected override void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
        var recipients = PdaMessaging.GetClientRecipients((ent.Owner, ent.Comp2)).ToArray();
        var state = new ChatUiState(recipients);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
