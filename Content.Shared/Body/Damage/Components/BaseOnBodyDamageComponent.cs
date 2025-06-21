using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Damage.Components;

public abstract partial class BaseOnBodyDamageComponent : Component
{
    [DataField]
    public HashSet<BodyDamageState> RequiredStates = new()
    {
        BodyDamageState.Alive,
        BodyDamageState.Wounded,
        BodyDamageState.Dead
    };

    [DataField]
    public FixedPoint2 MinDamage = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 MaxDamage = FixedPoint2.MaxValue;
}
