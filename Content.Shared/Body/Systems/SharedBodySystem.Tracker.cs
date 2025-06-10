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
        SubscribeLocalEvent<BodyTrackerComponent, OrganAddedBodyEvent>(OnTrackerOrganAdded);
        SubscribeLocalEvent<BodyTrackerComponent, OrganRemovedBodyEvent>(OnTrackerOrganRemoved);

        SubscribeLocalEvent<BodyTrackerComponent, BodyPartAddedEvent>(OnTrackerBodyPartAdded);
        SubscribeLocalEvent<BodyTrackerComponent, BodyPartRemovedEvent>(OnTrackerBodyPartRemoved);
    }

    private void OnTrackerOrganAdded(Entity<BodyTrackerComponent> ent, ref OrganAddedBodyEvent args)
    {
        AddTracker(ent, args.Organ);
    }

    private void OnTrackerOrganRemoved(Entity<BodyTrackerComponent> ent, ref OrganRemovedBodyEvent args)
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

            data.Add(toAdd, newData);

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
        foreach (var newData in RegisterNewTracker<T>(ent!))
            yield return newData;
    }

    private IEnumerable<Entity<T>> RegisterNewTracker<T>(Entity<BodyTrackerComponent> ent) where T : IComponent, new()
    {
        var componentName = Factory.GetComponentName<T>();

        Dictionary<EntityUid, IComponent> newTrackers = [];
        foreach (var (organ, comp, _) in GetBodyOrganEntityComps<T>(ent.Owner))
        {
            newTrackers.Add(organ, comp);
            yield return (organ, comp);
        }

        foreach (var (bodyPart, _) in GetBodyChildren(ent.Owner))
        {
            var component = Factory.GetRegistration(componentName);
            if (!EntityManager.TryGetComponent(bodyPart, component, out var comp))
                continue;

            newTrackers.Add(bodyPart, (T)comp);
            yield return (bodyPart, (T)comp);
        }

        ent.Comp.Trackers.Add(componentName, newTrackers);
    }
}
