using Content.Shared.Arcade.Racer.Systems;
using Content.Shared.Maths;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(RacerArcadeObjectPhysicsSystem))]
public sealed partial class RacerArcadeObjectPhysicsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, RacerArcadePhysicsShapeEntry> Shapes = [];

    [ViewVariables, AutoNetworkedField]
    public Vector3 Velocity;

    [ViewVariables, AutoNetworkedField]
    public Vector3 AngularVelocity;

    [ViewVariables, AutoNetworkedField]
    public Box3 CachedAABB;

    [ViewVariables, AutoNetworkedField]
    public GameTick LastCachedAABB;

    [ViewVariables, AutoNetworkedField]
    public int AllLayers;

    [ViewVariables, AutoNetworkedField]
    public int AllMasks;

}
