using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed partial class OrganMissingDamageSystem : BaseBodyTrackedSystem
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

            Dictionary<EntityUid, Dictionary<int, bool>> nextDamageUpdated = [];

            foreach (var (organ, entries) in component.Organs)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var data = entries[i];
                    if (data.NextDamage > _timing.CurTime)
                        continue;

                    var passedGrace = false;

                    if (!data.PassedDamageGrace)
                    {
                        if (_timing.CurTime < data.DamageGrace)
                            continue;

                        passedGrace = true;
                    }

                    if (nextDamageUpdated.TryGetValue(organ, out var toAdd))
                    {
                        toAdd.Add(i, passedGrace);
                    }
                    else
                    {
                        toAdd = [];
                        toAdd.Add(i, passedGrace);
                    }

                    _damageable.TryChangeDamage(uid, data.Damage, interruptsDoAfters: false);
                }
            }

            foreach (var (organ, array) in nextDamageUpdated)
            {
                var entries = component.Organs[organ];
                foreach (var (index, passedDamageGrace) in array)
                {
                    var data = entries[index];
                    data.NextDamage += data.DamageDelay;

                    if (passedDamageGrace)
                        data.PassedDamageGrace = true;
                }

                component.Organs[organ] = entries;
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
        if (!ent.Comp.Organs.Remove(args.Tracked.Owner, out var entries))
            return;

        if (entries.Any(entry => entry.DamageDelay == ent.Comp.DamageDelay))
        {
            ent.Comp.DamageDelay = ent.Comp.Organs.Any()
                ? ent.Comp.Organs.Values.SelectMany(arr => arr).Min(entry => entry.DamageDelay)
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
        var currentTime = _timing.CurTime;
        var newEntries = new OrganMissingDamageContainerEntry[trackedComp.Entries.Length];

        TimeSpan? minDelay = null;

        for (var i = 0; i < trackedComp.Entries.Length; i++)
        {
            var entry = trackedComp.Entries[i];
            var graceTime = entry.GraceTime + currentTime;
            var nextDamage = entry.DamageDelay + currentTime;

            newEntries[i] = new OrganMissingDamageContainerEntry(entry.Damage, graceTime, entry.DamageDelay, nextDamage);

            if (minDelay == null || entry.DamageDelay < minDelay)
                minDelay = entry.DamageDelay;
        }

        ent.Comp.Organs[args.Tracked.Owner] = newEntries;

        if (minDelay < ent.Comp.DamageDelay)
            ent.Comp.DamageDelay = minDelay.Value;

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
