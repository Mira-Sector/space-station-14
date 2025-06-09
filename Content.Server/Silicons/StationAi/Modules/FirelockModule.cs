using Content.Server.Atmos.Monitor.Components;
using Content.Shared.Charges.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.StationAi.Modules;

namespace Content.Server.Silicons.StationAi.Modules;

public sealed class FirelockModuleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCanHackComponent, StationAiFirelockEvent>(OnAction);
    }

    private void OnAction(EntityUid uid, StationAiCanHackComponent component, StationAiFirelockEvent args)
    {
        if (args.Handled)
            return;

        var aiXForm = Transform(uid);
        var query = EntityQueryEnumerator<FirelockComponent, AtmosAlarmableComponent>();

        while (query.MoveNext(out var firelockUid, out var _, out var alarmComp))
        {
            var firelockXForm = Transform(firelockUid);

            if (aiXForm.MapUid != firelockXForm.MapUid)
                continue;

            alarmComp.IngoreWirePanel = true;
            alarmComp.IgnoreAlarms = true;
            args.Handled = true;
        }

        if (!args.Handled)
            return;

        if (TryComp<LimitedChargesComponent>(args.Action.Owner, out var charges) && charges.LastCharges > 0)
            return;

        EntityManager.DeleteEntity(args.Action);
    }
}

