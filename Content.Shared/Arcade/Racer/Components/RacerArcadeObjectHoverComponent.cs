using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RacerArcadeObjectHoverComponent : Component
{
    [DataField]
    public float TargetHeight = 16f;

    [DataField]
    public float Strength = 10f;

    [DataField]
    public float Damping = 2f;

    [DataField]
    public float MaxForce = 1000f;
}
