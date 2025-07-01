using Content.Server.Body.Damage.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Damage.Systems;

namespace Content.Server.Body.Damage.Systems;

public sealed partial class MetabolizerDelayOnBodyDamageSystem : BaseDelayOnBodyDamageSystem<MetabolizerDelayOnBodyDamageComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetabolizerDelayOnBodyDamageComponent, GetMetabolizingUpdateDelay>(OnGetDelay);
    }

    private void OnGetDelay(Entity<MetabolizerDelayOnBodyDamageComponent> ent, ref GetMetabolizingUpdateDelay args)
    {
        if (!CanDoEffect(ent))
            return;

        if (!TryGetAdditionalDelay(ent, args.TotalDelay, args.StartingDelay, out var delay))
            return;

        args.AdditionalDelay += delay.Value;
    }
}
