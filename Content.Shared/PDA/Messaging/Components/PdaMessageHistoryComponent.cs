using Content.Shared.PDA.Messaging.Messages;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Messaging.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaMessagingHistoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public IChatMessage[] Messages { get; set; }

    [DataField, AutoNetworkedField]
    public int MaxHistory { get; set; }

    [DataField, AutoNetworkedField]
    public int MessageCount { get; set; }
}
