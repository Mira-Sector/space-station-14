using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodyPartThresholdsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<WoundState, FixedPoint2> Thresholds = new();

    [DataField, AutoNetworkedField]
    public WoundState CurrentState = WoundState.Healthy;
}

[Serializable, NetSerializable]
public enum WoundState
{
    Healthy,
    Damaged,
    Dead
}
