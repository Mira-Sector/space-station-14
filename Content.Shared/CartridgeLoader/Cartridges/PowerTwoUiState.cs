using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed partial class PowerTwoUiState(PowerTwoGameState gameState, int?[] grid, Vector2i gridSize, int maxValue, TimeSpan startTime, bool playSound) : BoundUserInterfaceState
{
    public readonly PowerTwoGameState GameState = gameState;
    public readonly int?[] Grid = grid;
    public readonly Vector2i GridSize = gridSize;
    public readonly int MaxValue = maxValue;
    public readonly TimeSpan StartTime = startTime;
    public readonly bool PlaySound = playSound;
}
