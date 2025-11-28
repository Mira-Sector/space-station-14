using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RacerArcadeObjectGravityComponent : Component
{
    [DataField]
    public Vector3 Force = new(0, 0, -0.5f);
}
