using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modules.ModSuit.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModSuitUserInterfaceComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionModSuitViewUI";

    [ViewVariables]
    public EntityUid? Action;
}
