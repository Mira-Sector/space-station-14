using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostVisualsComponent : Component
{
    [DataField]
    public HashSet<MarkingCategories> MarkingsToTransfer = new();

    [DataField]
    public float MarkingsAlpha;

    [DataField]
    public Dictionary<string, DisplacementData> LayerDisplacements = new();

    [ViewVariables]
    public HashSet<string> RevealedLayers = new();

    [ViewVariables, AutoNetworkedField]
    public Dictionary<Enum, HashSet<string>> LayersModified = new();

    [DataField]
    public bool TransferColor = true;
}

[Serializable, NetSerializable]
public enum GhostVisuals
{
    Color,
    Species,
    Layer,
}
