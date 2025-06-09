using Content.Shared.Actions;
using Content.Shared.Charges.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Shared.Silicons.StationAi.Modules;

public sealed class LockdownModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doors = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiCanHackComponent, StationAiLockdownEvent>(OnAction);
    }

    private void OnAction(EntityUid uid, StationAiCanHackComponent component, StationAiLockdownEvent args)
    {
        if (args.Handled)
            return;

        var aiXForm = Transform(uid);

        var doorQuery = EntityQueryEnumerator<DoorComponent>();

        while (doorQuery.MoveNext(out var doorUid, out var doorComp))
        {
            var doorXForm = Transform(doorUid);

            if (aiXForm.MapUid != doorXForm.MapUid)
                continue;

            if (!_doors.TryClose(doorUid, doorComp, uid))
                continue;

            args.Handled = true;
        }

        // others are only here so we only bolt doors that are applicable
        var boltQuery = EntityQueryEnumerator<DoorBoltComponent, DoorComponent>();

        while (boltQuery.MoveNext(out var boltUid, out var boltComp, out var _))
        {
            // already what we want
            if (boltComp.BoltsDown)
                continue;

            var boltXForm = Transform(boltUid);

            if (aiXForm.MapUid != boltXForm.MapUid)
                continue;

            args.Handled = true;

            _doors.SetBoltsDown((boltUid, boltComp), true, uid, true);

            var boltDelay = EnsureComp<DelayDoorBoltComponent>(boltUid);
            boltDelay.Delay = args.ResetDelay;
            boltDelay.Bolt = false;
            boltDelay.Enabled = true;
            Dirty(boltUid, boltDelay);
        }

        if (!args.Handled)
            return;

        if (TryComp<LimitedChargesComponent>(args.Action.Owner, out var charges) && charges.LastCharges > 0)
            return;

        EntityManager.DeleteEntity(args.Action);
    }
}

public sealed partial class StationAiLockdownEvent : InstantActionEvent
{
    [DataField]
    public TimeSpan ResetDelay;

    public StationAiLockdownEvent(TimeSpan delay)
    {
        ResetDelay = delay;
    }
}
