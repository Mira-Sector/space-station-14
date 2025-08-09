using Content.Shared.Body.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Body.Damage.Components;

[RegisterComponent]
public sealed partial class VomitOnBodyDamageComponent : BaseOnBodyDamageComponent
{
    [DataField]
    public bool TriggeredOnDigestion = true;

    [DataField]
    public float MinProb;

    [DataField]
    public float MaxProb = 1f;

    [DataField]
    public FixedPoint2? ScaleProbToDamage;

    [ViewVariables]
    public float CurrentProb;
}
