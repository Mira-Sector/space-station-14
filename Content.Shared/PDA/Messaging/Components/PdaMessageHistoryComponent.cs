using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Messaging.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaMessagingHistoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<IPdaChatRecipient, TimeSpan> LastMessage = [];

    [DataField, AutoNetworkedField]
    public Dictionary<IPdaChatRecipient, BasePdaChatMessage[]> Messages = [];

    [DataField]
    public int MaxHistory = 64;

    [DataField, AutoNetworkedField]
    public Dictionary<IPdaChatRecipient, int> MessageCount = [];

    [DataField, AutoNetworkedField]
    public Dictionary<IPdaChatRecipient, int> MessageIndex = [];
}
