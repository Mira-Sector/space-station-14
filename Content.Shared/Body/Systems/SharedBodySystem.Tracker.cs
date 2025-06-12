using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using JetBrains.Annotations;
using System.Linq;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeTracker()
    {
        SubscribeLocalEvent<BodyTrackerComponent, OrganAddedBodyEvent>(OnTrackerOrganBodyAdded);
        SubscribeLocalEvent<BodyTrackerComponent, OrganRemovedBodyEvent>(OnTrackerOrganBodyRemoved);

        SubscribeLocalEvent<BodyTrackerComponent, OrganAddedLimbEvent>(OnTrackerOrganLimbAdded);
        SubscribeLocalEvent<BodyTrackerComponent, OrganRemovedLimbEvent>(OnTrackerOrganLimbRemoved);

        SubscribeLocalEvent<BodyTrackerComponent, BodyPartAddedEvent>(OnTrackerBodyPartAdded);
        SubscribeLocalEvent<BodyTrackerComponent, BodyPartRemovedEvent>(OnTrackerBodyPartRemoved);
    }

    private void OnTrackerOrganBodyAdded(Entity<BodyTrackerComponent> ent, ref OrganAddedBodyEvent args)
    {
        AddTracker(ent, args.Organ);
    }

    private void OnTrackerOrganBodyRemoved(Entity<BodyTrackerComponent> ent, ref OrganRemovedBodyEvent args)
    {
        RemoveTracker(ent, args.Organ);
    }

    private void OnTrackerOrganLimbAdded(Entity<BodyTrackerComponent> ent, ref OrganAddedLimbEvent args)
    {
        AddTracker(ent, args.Organ);
    }

    private void OnTrackerOrganLimbRemoved(Entity<BodyTrackerComponent> ent, ref OrganRemovedLimbEvent args)
    {
        RemoveTracker(ent, args.Organ);
    }

    private void OnTrackerBodyPartAdded(Entity<BodyTrackerComponent> ent, ref BodyPartAddedEvent args)
    {
        AddTracker(ent, args.Part);
    }

    private void OnTrackerBodyPartRemoved(Entity<BodyTrackerComponent> ent, ref BodyPartRemovedEvent args)
    {
        RemoveTracker(ent, args.Part);
    }

    private void AddTracker(Entity<BodyTrackerComponent> ent, EntityUid toAdd)
    {
        foreach (var (componentName, data) in ent.Comp.Trackers)
        {
            var component = Factory.GetRegistration(componentName);
            if (!EntityManager.TryGetComponent(toAdd, component, out var newData))
                continue;

            if (!data.TryAdd(toAdd, newData))
                continue;

            var ev = new BodyTrackerAdded((toAdd, newData), (uint)data.Count + 1, componentName);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    private void RemoveTracker(Entity<BodyTrackerComponent> ent, EntityUid toRemove)
    {
        foreach (var (componentName, data) in ent.Comp!.Trackers)
        {
            if (!data.Remove(toRemove, out var component))
                continue;

            var ev = new BodyTrackerRemoved((toRemove, component), (uint)data.Count - 1, componentName);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    [PublicAPI]
    public void RegisterTracker<T>(Entity<BodyTrackerComponent?> ent) where T : IComponent, new()
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            EnsureComp(ref ent);

        var componentName = Factory.GetComponentName<T>();
        if (ent.Comp!.Trackers.ContainsKey(componentName))
            return;

        RegisterNewTracker<T>(ent!);
    }

    [PublicAPI]
    public uint GetTrackerCount<T>(Entity<BodyTrackerComponent?> ent) where T : IComponent, new()
    {
        return (uint)GetTrackers<T>(ent).Count();
    }

    [PublicAPI]
    public IEnumerable<Entity<T>> GetTrackers<T>(Entity<BodyTrackerComponent?> ent) where T : IComponent, new()
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            EnsureComp(ref ent);

        var componentName = Factory.GetComponentName<T>();
        if (ent.Comp!.Trackers.TryGetValue(componentName, out var trackers))
        {
            foreach (var (tracked, comp) in trackers)
                yield return (tracked, (T)comp);

            yield break;
        }

        // couldnt find it
        // retroactively update the entry
        RegisterNewTracker<T>(ent!);

        foreach (var (tracked, comp) in ent.Comp!.Trackers[componentName])
            yield return (tracked, (T)comp);
    }

    private void RegisterNewTracker<T>(Entity<BodyTrackerComponent> ent) where T : IComponent, new()
    {
        if (TryComp<BodyComponent>(ent.Owner, out var bodyComp))
            RegisterNewTrackerBody<T>((ent.Owner, ent.Comp, bodyComp));
        else if (TryComp<BodyPartComponent>(ent.Owner, out var bodyPartComp))
            RegisterNewTrackerLimb<T>((ent.Owner, ent.Comp, bodyPartComp));
    }

    private void RegisterNewTrackerBody<T>(Entity<BodyTrackerComponent, BodyComponent> ent) where T : IComponent, new()
    {
        var componentName = Factory.GetComponentName<T>();
        var query = GetEntityQuery<T>();

        Dictionary<EntityUid, IComponent> newTrackers = [];
        foreach (var (organ, comp, _) in GetBodyOrganEntityComps<T>((ent.Owner, ent.Comp2)))
            newTrackers.Add(organ, comp);

        foreach (var (bodyPart, _) in GetBodyChildren(ent.Owner, ent.Comp2))
        {
            if (query.TryGetComponent(bodyPart, out var comp))
                newTrackers.Add(bodyPart, comp);
        }

        ent.Comp1.Trackers.Add(componentName, newTrackers);
    }

    private void RegisterNewTrackerLimb<T>(Entity<BodyTrackerComponent, BodyPartComponent> ent) where T : IComponent, new()
    {
        var componentName = Factory.GetComponentName<T>();
        var query = GetEntityQuery<T>();

        Dictionary<EntityUid, IComponent> newTrackers = [];
        foreach (var (organ, _) in GetPartOrgans(ent.Owner, ent.Comp2))
        {
            if (query.TryGetComponent(organ, out var comp))
                newTrackers.Add(organ, comp);
        }

        ent.Comp1.Trackers.Add(componentName, newTrackers);
    }
}
