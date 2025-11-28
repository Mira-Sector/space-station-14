using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RacerArcadeObjectComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Vector3 Position;

    [ViewVariables, AutoNetworkedField]
    public Quaternion Rotation;
}
