using Content.Shared.DisplacementMap;

namespace Content.Client.Atmos.Components;

[RegisterComponent]
public sealed partial class PipeLayerVisualsComponent : Component
{
    [DataField]
    public Dictionary<int, DisplacementData> Displacements = new();

    [DataField(required: true)]
    public HashSet<string> Layers = new();

    [DataField]
    public bool ChangeDrawDepth = true;

    [ViewVariables]
    public HashSet<string> RevealedLayers = new();

    [ViewVariables]
    public int LastLayer;
}
