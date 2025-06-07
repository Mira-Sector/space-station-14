using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.StationAi.Modules;

namespace Content.Server.Silicons.StationAi.Modules;

public sealed class OverloadModuleSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCanHackComponent, StationAiOverloadEvent>(OnAction);
        SubscribeLocalEvent<OverloadedComponent, TriggerEvent>(OnTrigger);
    }

    private void OnAction(EntityUid uid, StationAiCanHackComponent component, StationAiOverloadEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<LightningTargetComponent>(args.Target))
            return;

        EnsureComp<OverloadedComponent>(args.Target, out var overloadedComp);
        _trigger.HandleTimerTrigger(args.Target, uid, args.Delay, args.BeepInterval, 0f, args.BeepSound);

        args.Handled = true;

        if (TryComp<LimitedChargesComponent>(args.Action.Owner, out var charges) && charges.LastCharges > 0)
            return;

        EntityManager.DeleteEntity(args.Action);
    }

    private void OnTrigger(EntityUid uid, OverloadedComponent component, TriggerEvent args)
    {
        if (args.User == null)
            return;

        var ev = new HitByLightningEvent(args.User.Value, uid);
        RaiseLocalEvent(uid, ref ev);
    }
}
