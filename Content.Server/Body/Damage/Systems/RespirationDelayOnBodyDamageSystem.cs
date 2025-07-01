using Content.Server.Body.Components;
using Content.Server.Body.Damage.Components;
using Content.Shared.Body.Damage.Systems;

namespace Content.Server.Body.Damage.Systems;

public sealed partial class RespirationDelayOnBodyDamageSystem : BaseDelayOnBodyDamageSystem<RespirationDelayOnBodyDamageComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RespirationDelayOnBodyDamageComponent, GetRespiratingUpdateDelay>(OnGetRespiratingDelay);
    }

    private void OnGetRespiratingDelay(Entity<RespirationDelayOnBodyDamageComponent> ent, ref GetRespiratingUpdateDelay args)
    {
        if (!CanDoEffect(ent))
            return;

        if (!TryGetAdditionalDelay(ent, args.TotalDelay, args.SourceDelay, out var delay))
            return;

        args.AdditionalDelay += delay.Value;
    }
}
