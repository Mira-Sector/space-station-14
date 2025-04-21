using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Instruments;

[RegisterComponent, NetworkedComponent]
public sealed partial class HeadphonesComponent : Component
{
    [DataField("action")]
    public EntProtoId ActionId = "ActionInstrument";

    [ViewVariables]
    public EntityUid? Action;

    [ViewVariables]
    public bool IsWorn = false;
}
