using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Events;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class DefibrillationDisableOnBodyDamageSystem : BaseOnBodyDamageSystem<DefibrillationDisableOnBodyDamageComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefibrillationDisableOnBodyDamageComponent, OrganCanDefibrillateEvent>(OnCanDefib);
    }

    private void OnCanDefib(Entity<DefibrillationDisableOnBodyDamageComponent> ent, ref OrganCanDefibrillateEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanDoEffect(ent))
            return;

        args.Cancel();
        args.Reason = ent.Comp.Reason;
    }
}
