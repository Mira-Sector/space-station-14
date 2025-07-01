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

            foreach (var (_, damage) in component.Organs)
                _damageable.TryChangeDamage(uid, damage, interruptsDoAfters: false);

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
        ent.Comp.Organs.Remove(args.Tracked.Owner);
        Dirty(ent);
    }

    private void OnTrackerRemoved(Entity<OrganMissingDamageContainerComponent> ent, ref BodyTrackerRemoved args)
    {
        //we dont just fetch the componment as the organ may get deleted and we should still damage
        ent.Comp.Organs.Add(args.Tracked.Owner, ((OrganMissingDamageComponent)args.Tracked.Comp).Damage);
        Dirty(ent);
    }

    private bool CanDamage(Entity<OrganMissingDamageContainerComponent> ent)
    {
        if (_mobState.IsDead(ent.Owner))
            return false;

        if (TryComp<BodyPartComponent>(ent.Owner, out var bodyPart))
        {
            // body will handle it separtely
            if (bodyPart.Body is { })
                return false;
        }

        return true;
    }
}
