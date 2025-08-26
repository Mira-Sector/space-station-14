using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Messaging.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaMessagingClientComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public ChatRecipientProfile Profile;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Server;

    [ViewVariables, AutoNetworkedField]
    public HashSet<IChatRecipient> AvailableRecipients = [];
}
