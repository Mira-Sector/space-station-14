using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbentToggleComponent : Component
{
    [DataField]
    public string? ToggleActionId = "ActionAbsorbent";

    [ViewVariables]
    public EntityUid? AbsorbentAction;

    [DataField]
    public bool Enabled = false;

    [DataField]
    public bool CanSlip = true;
}

public sealed partial class AbsorbentActionEvent : InstantActionEvent;
