using Content.Shared.Body.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(BodyDamageThresholdsSystem), Other = AccessPermissions.ReadExecute)]
public sealed partial class BodyDamageThresholdsComponent : Component
{
    [DataField, AutoNetworkedField]
    public BodyDamageState CurrentState = BodyDamageState.Alive;

    [DataField(required: true), AutoNetworkedField]
    public OrderedDictionary<BodyDamageState, FixedPoint2> Thresholds = [];

    [DataField, AutoNetworkedField]
    public bool PreventFurtherDamage = true;
}

[Serializable, NetSerializable]
public enum BodyDamageState : byte
{
    Alive,
    Wounded,
    Dead
}

[Serializable, NetSerializable]
public enum BodyDamageThresholdVisuals : byte
{
    State
}
