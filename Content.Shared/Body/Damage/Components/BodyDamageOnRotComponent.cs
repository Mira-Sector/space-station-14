using Content.Shared.Body.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BodyDamageOnRotComponent : Component
{
    [DataField]
    public int MinRotStage = int.MinValue;

    [DataField]
    public int MaxRotStage = int.MaxValue;

    [DataField(required: true)]
    public FixedPoint2 FullyRottenDamage;

    [ViewVariables, AutoNetworkedField, Access(typeof(BodyDamageOnRotSystem))]
    public float LastDamagePercentage;
}
