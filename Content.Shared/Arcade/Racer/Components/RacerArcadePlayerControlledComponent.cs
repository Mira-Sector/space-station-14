using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RacerArcadePlayerControlledComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Controller;

    [DataField]
    public Vector3 CameraOffset;
}
