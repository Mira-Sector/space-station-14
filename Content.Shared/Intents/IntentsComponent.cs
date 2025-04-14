using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Intents;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class IntentsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<Intent> SelectableIntents = new();

    [DataField("defaultIntent"), AutoNetworkedField]
    public Intent SelectedIntent;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? SelectionAction;

    [DataField, AutoNetworkedField]
    public ResPath Sprites = new ResPath("/Textures/Interface/intents.rsi");
}
