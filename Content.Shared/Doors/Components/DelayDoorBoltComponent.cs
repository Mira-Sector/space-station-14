using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DelayDoorBoltComponent : Component
{
    [DataField]
    public bool Enabled = false;

    [DataField]
    public bool Repeatable = false;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3f);

    [DataField]
    public bool Bolt;

    [ViewVariables]
    public TimeSpan NextBolt;
}
