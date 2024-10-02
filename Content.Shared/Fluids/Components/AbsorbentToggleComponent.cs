using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbentToggleComponent : Component
{
    public string ToggleActionId = "ActionAbsorbent";

    [ViewVariables]
    public bool Enabled = false;

    [ViewVariables]
    public EntityUid? AbsorbentAction;
}

public sealed partial class AbsorbentActionEvent : InstantActionEvent;
