using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.GameStates;

namespace Content.Shared.PDA.Messaging.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PdaMessagingHistoryComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<BasePdaChatMessageable, TimeSpan> LastMessage = [];

    [DataField, AutoNetworkedField]
    public Dictionary<BasePdaChatMessageable, BasePdaChatMessage[]> Messages = [];

    [DataField]
    public int MaxHistory = 64;

    [DataField, AutoNetworkedField]
    public Dictionary<BasePdaChatMessageable, int> MessageCount = [];

    [DataField, AutoNetworkedField]
    public Dictionary<BasePdaChatMessageable, int> MessageIndex = [];
}
