using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RacerArcadeObjectGravityComponent : Component
{
    [DataField]
    public float Acceleration = -9.8f;
}
