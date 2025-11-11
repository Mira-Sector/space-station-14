using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Messaging.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaMessagingServerComponent : Component
{
    public const string IdPrefix = "SRV";

    [ViewVariables, AutoNetworkedField]
    public Dictionary<PdaChatRecipientProfile, EntityUid?> Profiles = [];

    [ViewVariables, AutoNetworkedField]
    public string Id;
}
