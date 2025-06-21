using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;

namespace Content.Shared.Body.Systems;

public sealed partial class OrganRequiredForDefibrillationSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public static readonly LocId NoOrganReason = "defibrillator-heart-missing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganRequiredForDefibrillationComponent, OrganAddedToBodyEvent>(OnOrganAdded);

        SubscribeLocalEvent<OrganRequiredForDefibrillationContainerComponent, DefibrillateAttemptEvent>(OnDefibAttempt);
    }

    private void OnOrganAdded(Entity<OrganRequiredForDefibrillationComponent> ent, ref OrganAddedToBodyEvent args)
    {
        EnsureComp<OrganRequiredForDefibrillationContainerComponent>(args.Body);
        _body.RegisterTracker<OrganRequiredForDefibrillationComponent>(args.Body);
    }

    private void OnDefibAttempt(Entity<OrganRequiredForDefibrillationContainerComponent> ent, ref DefibrillateAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        LocId? organReason = null;
        foreach (var tracker in _body.GetTrackers<OrganRequiredForDefibrillationComponent>(ent.Owner))
        {
            if (!IsEnabled(tracker))
                return;

            organReason = tracker.Comp.DisableReason;
        }

        args.Cancel();
        args.Reason = organReason ?? NoOrganReason;
    }

    private bool IsEnabled(EntityUid uid)
    {
        if (!_itemToggle.IsActivated(uid))
            return false;

        return true;
    }
}
