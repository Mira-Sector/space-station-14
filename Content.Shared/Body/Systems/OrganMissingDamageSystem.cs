using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed partial class OrganMissingDamageSystem : BaseBodyTrackedSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<OrganComponent> _organQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganMissingDamageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OrganMissingDamageComponent, OrganInitEvent>(OnOrganInit);

        SubscribeTrackerAdded<OrganMissingDamageContainerComponent, OrganMissingDamageComponent>(OnTrackerAdded);
        SubscribeTrackerRemoved<OrganMissingDamageContainerComponent, OrganMissingDamageComponent>(OnTrackerRemoved);

        _organQuery = GetEntityQuery<OrganComponent>();
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

            List<(OrganMissingDamageContainerEntry, bool)> nextDamageUpdated = [];

            foreach (var entry in component.Organs)
            {
                if (entry.NextDamage > _timing.CurTime)
                    continue;

                var passedGrace = !entry.PassedDamageGrace && _timing.CurTime > entry.DamageGrace;
                nextDamageUpdated.Add((entry, passedGrace));

                var damageToDeal = entry.Damage;
                if (entry.CapToOrganType)
                {
                    damageToDeal = new();
                    foreach (var (damageType, damageValue) in entry.Damage.DamageDict)
                    {
                        if (component.OrganTypeCaps[entry.OrganType].DamageDict.TryGetValue(damageType, out var existingDamageValue) && existingDamageValue > 0)
                            damageToDeal.DamageDict[damageType] = FixedPoint2.Max(0, damageValue - existingDamageValue);
                        else
                            damageToDeal.DamageDict[damageType] = damageValue;
                    }
                }

                if (_damageable.TryChangeDamage(uid, damageToDeal, interruptsDoAfters: false) is not { } addedDamage)
                    continue;

                component.OrganTypeCaps[entry.OrganType] += addedDamage;
            }

            foreach (var (entry, passedDamageGrace) in nextDamageUpdated)
            {
                var newEntry = new OrganMissingDamageContainerEntry(entry.Organ, entry.Damage, entry.DamageGrace, entry.DamageDelay, entry.NextDamage, entry.DamageOn, entry.OrganType, entry.CapToOrganType);
                newEntry.NextDamage += entry.DamageDelay;

                if (passedDamageGrace)
                    newEntry.PassedDamageGrace = true;

                component.Organs.Remove(entry);
                component.Organs.Add(newEntry);
            }

            Dirty(uid, component);
        }
    }

    private void OnInit(Entity<OrganMissingDamageComponent> ent, ref ComponentInit args)
    {
        foreach (var entry in ent.Comp.Entries)
        {
            if (!ent.Comp.DamageTypeCount.TryGetValue(entry.DamageOn, out var typeCount))
                ent.Comp.DamageTypeCount[entry.DamageOn] = 1;
            else
                ent.Comp.DamageTypeCount[entry.DamageOn] = typeCount++;
        }

        /*
         * ensure they all have a value
         * can just index the dictionary rather than do a tryget later
        */
        foreach (var type in Enum.GetValues<OrganMissingDamageType>())
        {
            if (!ent.Comp.DamageTypeCount.ContainsKey(type))
                ent.Comp.DamageTypeCount[type] = 0;
        }
    }

    private void OnOrganInit(Entity<OrganMissingDamageComponent> ent, ref OrganInitEvent args)
    {
        EnsureComp<OrganMissingDamageContainerComponent>(args.Part.Owner, out var partComp);
        Body.RegisterTracker<OrganMissingDamageComponent>(args.Part.Owner);
        DamageSourceModify((args.Part.Owner, partComp), ent, OrganMissingDamageType.Added);

        EnsureComp<OrganMissingDamageContainerComponent>(args.Body.Owner, out var bodyComp);
        Body.RegisterTracker<OrganMissingDamageComponent>(args.Body.Owner);
        DamageSourceModify((args.Body.Owner, bodyComp), ent, OrganMissingDamageType.Added);
    }

    private void OnTrackerAdded(Entity<OrganMissingDamageContainerComponent> ent, ref BodyTrackerAdded args)
    {
        DamageSourceModify(ent, (args.Tracked.Owner, (OrganMissingDamageComponent)args.Tracked.Comp), OrganMissingDamageType.Added);
    }

    private void OnTrackerRemoved(Entity<OrganMissingDamageContainerComponent> ent, ref BodyTrackerRemoved args)
    {
        DamageSourceModify(ent, (args.Tracked.Owner, (OrganMissingDamageComponent)args.Tracked.Comp), OrganMissingDamageType.Missing);
    }

    private void DamageSourceModify(Entity<OrganMissingDamageContainerComponent> ent, Entity<OrganMissingDamageComponent> source, OrganMissingDamageType damageType)
    {
        /*
         * we dont just fetch the componment in the update loop
         * the organ may get deleted and we should still damage
        */
        var organType = _organQuery.Comp(source.Owner).OrganType;

        TimeSpan? minDelay = null;

        List<OrganMissingDamageContainerEntry> newEntries = [];
        foreach (var entry in source.Comp.Entries)
        {
            if (entry.DamageOn != damageType)
                continue;

            var graceTime = entry.GraceTime + _timing.CurTime;
            var nextDamage = entry.DamageDelay + _timing.CurTime;

            newEntries.Add(new OrganMissingDamageContainerEntry(GetNetEntity(source.Owner), entry.Damage, graceTime, entry.DamageDelay, nextDamage, entry.DamageOn, organType, entry.CapToOrganType));

            if (minDelay == null || entry.DamageDelay < minDelay)
                minDelay = entry.DamageDelay;
        }

        /*
         * remove any conflicting damage types
         * dont want to still be taking damage if the liver got replaced with someone elses
         * so check for any existing ones and remove them
        */
        List<OrganMissingDamageContainerEntry> toRemove = [];
        foreach (var entry in ent.Comp.Organs)
        {
            if (GetEntity(entry.Organ) == source.Owner)
            {
                toRemove.Add(entry);
                continue;
            }

            if (entry.OrganType != organType)
                continue;

            if (entry.DamageOn == damageType)
                continue;

            switch (damageType)
            {
                case OrganMissingDamageType.Added:
                    toRemove.Add(entry);
                    break;
                case OrganMissingDamageType.Missing:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        foreach (var entry in toRemove)
            ent.Comp.Organs.Remove(entry);

        foreach (var entry in newEntries)
            ent.Comp.Organs.Add(entry);

        if (!ent.Comp.OrganTypeCaps.ContainsKey(organType))
            ent.Comp.OrganTypeCaps.Add(organType, new());

        if (minDelay == null)
            ent.Comp.DamageDelay = OrganMissingDamageContainerComponent.DefaultDamageDelay;
        else if (minDelay < ent.Comp.DamageDelay)
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
