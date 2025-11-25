using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RacerArcadeObjectComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Vector3 Position;
}
