using Robust.Shared.Serialization;

namespace Content.Shared.Modules.ModSuit.UI;

[Serializable, NetSerializable]
public sealed class ModSuitComplexityBoundUserInterfaceState : BoundUserInterfaceState
{
    public (int Complexity, int MaxComplexity)? Complexity;

    public ModSuitComplexityBoundUserInterfaceState(int complexity, int maxComplexity)
    {
        Complexity = (complexity, maxComplexity);
    }

    public ModSuitComplexityBoundUserInterfaceState((int, int) complexity)
    {
        Complexity = complexity;
    }
}
