using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhostVisualsComponent : Component
{
    [DataField]
    public HashSet<MarkingCategories> MarkingsToTransfer = new();

    [DataField]
    public float MarkingsAlpha;

    [DataField]
    public bool TransferColor = true;
}

[Serializable, NetSerializable]
public enum GhostVisuals
{
    Color,
    Layer,
}
