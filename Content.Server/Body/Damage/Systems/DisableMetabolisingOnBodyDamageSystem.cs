using Content.Server.Body.Damage.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Damage.Systems;

namespace Content.Server.Body.Damage.Systems;

public sealed partial class DisableMetabolisingOnBodyDamageSystem : BaseToggleOnBodyDamageSystem<DisableMetabolisingOnBodyDamageComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisableMetabolisingOnBodyDamageComponent, GetMetabolizingUpdateDelay>(OnGetDelay);
    }

    private void OnGetDelay(Entity<DisableMetabolisingOnBodyDamageComponent> ent, ref GetMetabolizingUpdateDelay args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Enabled)
            args.Cancel();
    }
}
