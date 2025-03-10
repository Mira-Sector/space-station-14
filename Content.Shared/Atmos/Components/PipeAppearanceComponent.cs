using Robust.Shared.Utility;
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PipeAppearanceComponent : Component
{
    [DataField]
    public SpriteSpecifier.Rsi Sprite = new(new("Structures/Piping/Atmospherics/pipe.rsi"), "pipeConnector");

    [ViewVariables, AutoNetworkedField]
    public Dictionary<int, PipeDirection> ConnectedDirections = new();
}
