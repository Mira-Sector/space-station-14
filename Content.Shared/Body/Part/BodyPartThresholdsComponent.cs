using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBodySystem))]
public sealed partial class BodyPartThresholdsComponent : Component
{
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, WoundState> Thresholds = new();

    [DataField]
    public WoundState CurrentState = WoundState.Healthy;
}

public enum WoundState
{
    Healthy,
    Damaged,
    Dead
}
