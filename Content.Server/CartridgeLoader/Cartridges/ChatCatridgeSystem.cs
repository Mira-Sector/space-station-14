using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA.Messaging.Components;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class ChatCartridgeSystem : SharedChatCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;

    protected override void UpdateUi(Entity<ChatCartridgeComponent, PdaMessagingClientComponent?> ent, EntityUid loader)
    {
        if (!Resolve(ent.Owner, ref ent.Comp2))
            return;

        var state = new ChatUiState(ent.Comp2.AvailableRecipients);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state);
    }
}
