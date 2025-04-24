using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Intents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class IntentsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<IntentPrototype>> SelectableIntents = new();

    [DataField("defaultIntent"), AutoNetworkedField, Access(typeof(IntentSystem))]
    public ProtoId<IntentPrototype> SelectedIntent;

    [DataField, AutoNetworkedField]
    public EntProtoId SelectionActionId = "ActionIntentSelection";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? SelectionAction;
}
