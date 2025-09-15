using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChatCartridgeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<BasePdaChatMessageable, int> UnreadMessageCount = [];

    [ViewVariables, AutoNetworkedField]
    public BasePdaChatMessageable? SelectedRecipient;
}
