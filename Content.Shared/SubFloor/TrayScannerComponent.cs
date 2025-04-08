using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SubFloor;

[RegisterComponent, NetworkedComponent]
public sealed partial class TrayScannerComponent : Component
{
    /// <summary>
    ///     Whether the scanner is currently on.
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    ///     Radius in which the scanner will reveal entities. Centered on the <see cref="LastLocation"/>.
    /// </summary>
    [DataField]
    public float Range = 4f;

    [DataField]
    public bool CanToggleLayers = false;

    [DataField]
    public HashSet<int> ToggleableLayers = new()
    {
        -2,
        -1,
        0,
        1,
        2
    };

    [ViewVariables]
    public HashSet<int> RevealedLayers = new();
}

[Serializable, NetSerializable]
public sealed class TrayScannerState : ComponentState
{
    public bool Enabled;
    public float Range;
    public HashSet<int> RevealedLayers;

    public TrayScannerState(bool enabled, float range, HashSet<int> revealedLayers)
    {
        Enabled = enabled;
        Range = range;
        RevealedLayers = revealedLayers;
    }
}
