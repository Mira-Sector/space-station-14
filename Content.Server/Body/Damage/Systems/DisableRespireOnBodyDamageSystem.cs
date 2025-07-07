using Content.Server.Body.Damage.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Damage.Systems;

namespace Content.Server.Body.Damage.Systems;

public sealed partial class DisableRespireOnBodyDamageSystem : BaseToggleOnBodyDamageSystem<DisableRespireOnBodyDamageComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisableRespireOnBodyDamageComponent, CanRespireEvent>(OnCanRespire);
    }

    private void OnCanRespire(Entity<DisableRespireOnBodyDamageComponent> ent, ref CanRespireEvent args)
    {
        args.Enabled = ent.Comp.Enabled;
    }
}
