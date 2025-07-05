using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed class OrganMissingDamageSystem : BaseBodyTrackedSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganMissingDamageComponent, OrganInitEvent>(OnOrganInit);

        SubscribeTrackerAdded<OrganMissingDamageContainerComponent, OrganMissingDamageComponent>(OnTrackerAdded);
        SubscribeTrackerRemoved<OrganMissingDamageContainerComponent, OrganMissingDamageComponent>(OnTrackerRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OrganMissingDamageContainerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextDamage)
                continue;

            component.NextDamage += component.DamageDelay;

            if (!CanDamage((uid, component)))
            {
                Dirty(uid, component);
                continue;
            }

            Dictionary<EntityUid, bool> nextDamageUpdated = [];

            foreach (var (organ, data) in component.Organs)
            {
                if (data.NextDamage > _timing.CurTime)
                    continue;

                if (!data.PassedDamageGrace)
                {
                    if (_timing.CurTime < data.DamageGrace)
                        continue;

                    nextDamageUpdated.Add(organ, true);
                }
                else
                {
                    nextDamageUpdated.Add(organ, false);
                }

                _damageable.TryChangeDamage(uid, data.Damage, interruptsDoAfters: false);
            }

            foreach (var (organ, passedDamageGrace) in nextDamageUpdated)
            {
                var data = component.Organs[organ];
                data.NextDamage += data.DamageDelay;

                if (passedDamageGrace)
                    data.PassedDamageGrace = true;

                component.Organs[organ] = data;
            }

            Dirty(uid, component);
        }
    }

    private void OnOrganInit(Entity<OrganMissingDamageComponent> ent, ref OrganInitEvent args)
    {
        EnsureComp<OrganMissingDamageContainerComponent>(args.Part);
        Body.RegisterTracker<OrganMissingDamageComponent>(args.Part.Owner);

        EnsureComp<OrganMissingDamageContainerComponent>(args.Body);
        Body.RegisterTracker<OrganMissingDamageComponent>(args.Body.Owner);
    }

    private void OnTrackerAdded(Entity<OrganMissingDamageContainerComponent> ent, ref BodyTrackerAdded args)
    {
        ent.Comp.Organs.Remove(args.Tracked.Owner, out var removed);

        // recalculate the damage delay
        if (removed.DamageDelay == ent.Comp.DamageDelay)
        {
            ent.Comp.DamageDelay = ent.Comp.Organs.Any()
                ? ent.Comp.Organs.Values.Min(data => data.DamageDelay)
                : OrganMissingDamageContainerComponent.DefaultDamageDelay;
        }

        Dirty(ent);
    }

    private void OnTrackerRemoved(Entity<OrganMissingDamageContainerComponent> ent, ref BodyTrackerRemoved args)
    {
        /*
         * we dont just fetch the componment in the update loop
         * the organ may get deleted and we should still damage
        */
        var trackedComp = (OrganMissingDamageComponent)args.Tracked.Comp;
        var graceTime = trackedComp.GraceTime + _timing.CurTime;
        var nextDamage = trackedComp.DamageDelay + _timing.CurTime;
        var data = new OrganMissingDamageContainerEntry(trackedComp.Damage, graceTime, trackedComp.DamageDelay, nextDamage);
        ent.Comp.Organs.Add(args.Tracked.Owner, data);

        // always follow lowest common denominator
        if (trackedComp.DamageDelay < ent.Comp.DamageDelay)
            ent.Comp.DamageDelay = trackedComp.DamageDelay;

        Dirty(ent);
    }

    private bool CanDamage(Entity<OrganMissingDamageContainerComponent> ent)
    {
        if (TryComp<BodyPartComponent>(ent.Owner, out var bodyPart))
        {
            // body will handle it separtely
            if (bodyPart.Body != null)
                return false;
        }
        else
        {
            if (_mobState.IsDead(ent.Owner))
                return false;
        }

        return true;
    }
}
