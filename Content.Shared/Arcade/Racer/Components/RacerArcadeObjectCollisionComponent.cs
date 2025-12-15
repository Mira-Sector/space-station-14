using Content.Shared.Arcade.Racer.Systems;
using Content.Shared.Maths;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(RacerArcadeObjectCollisionSystem))]
public sealed partial class RacerArcadeObjectCollisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, RacerArcadeCollisionShapeEntry> Shapes = [];

    [ViewVariables, AutoNetworkedField]
    public Box3 CachedAABB;

    [ViewVariables, AutoNetworkedField]
    public int AllLayers;

    [ViewVariables, AutoNetworkedField]
    public int AllMasks;
}
