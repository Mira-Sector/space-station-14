using Content.Server.Body.Components;
using Content.Server.Body.Damage.Components;
using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Body.Damage.Systems;

public sealed partial class RespirationDelayOnBodyDamageSystem : BaseOnBodyDamageSystem<RespirationDelayOnBodyDamageComponent>
{
    [Dependency] private readonly BodyDamageThresholdsSystem _bodyDamageThresholds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RespirationDelayOnBodyDamageComponent, GetRespiratingUpdateDelay>(OnGetRespiratingDelay);
    }

    private void OnGetRespiratingDelay(Entity<RespirationDelayOnBodyDamageComponent> ent, ref GetRespiratingUpdateDelay args)
    {
        if (!CanDoEffect(ent))
            return;

        if (args.TotalDelay > ent.Comp.MaxDelay)
            return;

        if (!TryComp<BodyDamageThresholdsComponent>(ent.Owner, out var thresholdsComp))
            return;

        if (!thresholdsComp.Thresholds.ContainsKey(ent.Comp.TargetState))
            return;

        var maxAdditionalDelay = ent.Comp.MaxDelay - args.SourceDelay;

        if (thresholdsComp.CurrentState >= ent.Comp.TargetState)
        {
            args.AdditionalDelay += maxAdditionalDelay;
            return;
        }

        var distanceToThreshold = FixedPoint2.Abs(_bodyDamageThresholds.RelativeToState((ent.Owner, thresholdsComp), ent.Comp.TargetState));
        if (distanceToThreshold == FixedPoint2.Zero)
        {
            args.AdditionalDelay += maxAdditionalDelay;
            return;
        }

        var delayScalingFactor = MathF.Min(1, 1 / (float)distanceToThreshold);
        args.AdditionalDelay += maxAdditionalDelay * delayScalingFactor;
    }
}
