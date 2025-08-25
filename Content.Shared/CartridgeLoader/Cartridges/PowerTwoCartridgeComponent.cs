using Content.Shared.PDA;
using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerTwoCartridgeComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public PowerTwoGameState GameState;

    [DataField]
    public Vector2i GridSize = new(4, 4);

    [ViewVariables, AutoNetworkedField]
    public int?[] Grid;

    [DataField(required: true)]
    public Dictionary<int, float> StartingScores = [];

    [DataField]
    public int WinningScore = 2048;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan StartTime;

    [DataField]
    public Dictionary<PowerTwoGameState, Note[]> StateSongs = [];

    [ViewVariables, AutoNetworkedField]
    public bool PlaySounds = true;
}
