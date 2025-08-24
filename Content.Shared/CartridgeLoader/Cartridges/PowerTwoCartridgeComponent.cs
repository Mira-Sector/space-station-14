using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerTwoCartridgeComponent : Component
{
    [DataField]
    public Vector2i GridSize = new(8, 8);

    [ViewVariables, AutoNetworkedField]
    public int?[] Grid;

    [DataField]
    public Dictionary<int, float> StaringScores = new()
    {
        { 2, 0.9f },
        { 4, 0.1f }
    };

    [DataField]
    public int WinningScore = 2048;
}
