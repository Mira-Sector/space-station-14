using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Medical;

namespace Content.Shared.Body.Systems;

public sealed partial class OrganRequiredForDefibrillationSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public static readonly LocId NoOrganReason = "defibrillator-heart-missing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganRequiredForDefibrillationComponent, OrganInitEvent>(OnOrganInit);

        SubscribeLocalEvent<OrganRequiredForDefibrillationContainerComponent, DefibrillateAttemptEvent>(OnDefibAttempt);
    }

    private void OnOrganInit(Entity<OrganRequiredForDefibrillationComponent> ent, ref OrganInitEvent args)
    {
        EnsureComp<OrganRequiredForDefibrillationContainerComponent>(args.Body);
        _body.RegisterTracker<OrganRequiredForDefibrillationComponent>(args.Body.Owner);
    }

    private void OnDefibAttempt(Entity<OrganRequiredForDefibrillationContainerComponent> ent, ref DefibrillateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        LocId? organReason = null;
        foreach (var tracker in _body.GetTrackers<OrganRequiredForDefibrillationComponent>(ent.Owner))
        {
            if (IsEnabled(tracker, ent.Owner, out var reason))
                return;

            organReason = reason ?? tracker.Comp.DisableReason;
            break;
        }

        args.Cancel();
        args.Reason = organReason ?? NoOrganReason;
    }

    private bool IsEnabled(EntityUid uid, EntityUid body, out LocId? reason)
    {
        var ev = new OrganCanDefibrillateEvent(body);
        RaiseLocalEvent(uid, ref ev);
        reason = ev.Reason;
        return !ev.Cancelled;
    }
}
