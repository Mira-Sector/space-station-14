using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class PowerTwoUiState(int?[] grid, Vector2i gridSize, int maxValue) : BoundUserInterfaceState
{
    public readonly int?[] Grid = grid;
    public readonly Vector2i GridSize = gridSize;
    public readonly int MaxValue = maxValue;
}
