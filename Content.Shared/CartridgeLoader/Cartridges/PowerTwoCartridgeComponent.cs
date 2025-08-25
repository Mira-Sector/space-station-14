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

    [DataField]
    public Dictionary<int, float> StartingScores = new()
    {
        { 2, 0.9f },
        { 4, 0.1f }
    };

    [DataField]
    public int WinningScore = 2048;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan StartTime;
}
