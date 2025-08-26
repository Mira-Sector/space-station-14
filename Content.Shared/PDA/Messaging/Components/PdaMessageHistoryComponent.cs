using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Messaging.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaMessagingHistoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<IChatRecipient, TimeSpan> LastMessage = [];

    [DataField, AutoNetworkedField]
    public Dictionary<IChatRecipient, IChatMessage[]> Messages = [];

    [DataField, AutoNetworkedField]
    public int MaxHistory = 64;

    [DataField, AutoNetworkedField]
    public Dictionary<IChatRecipient, int> MessageCount = [];
}
