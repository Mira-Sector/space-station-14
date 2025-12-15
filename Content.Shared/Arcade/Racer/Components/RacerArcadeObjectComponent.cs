using Content.Shared.Arcade.Racer.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RacerArcadeObjectComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Vector3 Position;

    [ViewVariables, AutoNetworkedField]
    public Quaternion Rotation = Quaternion.Identity;

    [ViewVariables]
    [Access(typeof(SharedRacerArcadeSystem))]
    public Vector3 PreviousPosition;

    [ViewVariables]
    [Access(typeof(SharedRacerArcadeSystem))]
    public Quaternion PreviousRotation = Quaternion.Identity;

    [ViewVariables, AutoNetworkedField]
    [Access(typeof(SharedRacerArcadeSystem))]
    public EntityUid Arcade;
}
