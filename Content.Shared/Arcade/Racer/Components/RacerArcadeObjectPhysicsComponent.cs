using Content.Shared.Arcade.Racer.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(RacerArcadeObjectPhysicsSystem))]
public sealed partial class RacerArcadeObjectPhysicsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Mass = 1f;

    [DataField, AutoNetworkedField]
    public float MomentOfInertia = 0.5f;

    [DataField, AutoNetworkedField]
    public float LinearDrag = 0.1f;

    [DataField, AutoNetworkedField]
    public float AngularDrag = 0.1f;

    [DataField, AutoNetworkedField]
    public float Restitution = 0.2f;

    [ViewVariables]
    public Vector3 AccumulatedForce;

    [ViewVariables]
    public Vector3 AccumulatedTorque;

    [ViewVariables, AutoNetworkedField]
    public Vector3 Velocity;

    [ViewVariables, AutoNetworkedField]
    public Vector3 AngularVelocity;

    [ViewVariables]
    public Vector3 PredictedPosition;

    [ViewVariables]
    public Quaternion PredictedRotation = Quaternion.Identity;
}
