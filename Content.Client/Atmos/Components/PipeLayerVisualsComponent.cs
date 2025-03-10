using Content.Shared.DisplacementMap;
using System.Numerics;

namespace Content.Client.Atmos.Components;

[RegisterComponent]
public sealed partial class PipeLayerVisualsComponent : Component
{
    [DataField]
    public Dictionary<int, DisplacementData>? Displacements;

    [DataField]
    public HashSet<string>? DisplacementLayers;

    [DataField]
    public Dictionary<int, Vector2>? Offsets;

    [DataField]
    public HashSet<string>? OffsetLayers;

    [DataField]
    public bool ChangeDrawDepth = true;

    [ViewVariables]
    public HashSet<string> RevealedLayers = new();

    [ViewVariables]
    public int LastLayer;
}
