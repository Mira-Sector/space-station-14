using Content.Shared.Arcade.Racer.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(RacerArcadeObjectPhysicsSystem))]
public sealed partial class RacerArcadeObjectPhysicsComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Vector3 Velocity;

    [ViewVariables, AutoNetworkedField]
    public Vector3 AngularVelocity;

}
