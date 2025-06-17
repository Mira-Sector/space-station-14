using System.Diagnostics.CodeAnalysis;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private void InitializeOrgans()
    {
        SubscribeLocalEvent<OrganReplaceableComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<OrganReplaceableComponent, OrganSelectionButtonPressedMessage>(OnOrganButton);

        SubscribeLocalEvent<OrganComponent, IsRottingEvent>(OnOrganIsRotting);
    }

    private void OnUIOpened(EntityUid uid, OrganReplaceableComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnOrganButton(EntityUid uid, OrganReplaceableComponent component, OrganSelectionButtonPressedMessage args)
    {
        if (!Prototypes.TryIndex(args.OrganPrototype, out var organProto))
            return;

        var organUid = GetEntity(args.OrganId);

        if (organUid == null)
        {
            if (_hands.GetActiveItem(args.Actor) is not {} item)
                return;

            if (!AddOrganToFirstValidSlot(uid, item))
                return;

            UpdateUserInterface(uid, component);
        }
        else
        {
            if (!RemoveOrgan(organUid.Value))
                return;

            _hands.TryPickupAnyHand(args.Actor, organUid.Value);

            UpdateUserInterface(uid, component);
        }
    }

    private void UpdateUserInterface(EntityUid uid, OrganReplaceableComponent organReplaceable, BodyPartComponent? bodyPart = null)
    {
        if (!Resolve(uid, ref bodyPart))
            return;

        if (!_ui.IsUiOpen(uid, OrganSelectionUiKey.Key))
            return;

        Dictionary<ProtoId<OrganPrototype>, NetEntity?> organs = new();

        foreach (var organSlot in bodyPart.Organs.Values)
        {
            if (!Containers.TryGetContainer(uid, GetOrganContainerId(organSlot.Id), out var container))
                continue;

            EntityUid? organ = container.ContainedEntities.Count == 1 ? container.ContainedEntities[0] : null;

            organs.Add(organSlot.OrganType, GetNetEntity(organ));
        }

        _ui.SetUiState(uid, OrganSelectionUiKey.Key, new OrganSelectionBoundUserInterfaceState(organs));
    }

    private void OnOrganIsRotting(EntityUid uid, OrganComponent component, ref IsRottingEvent args)
    {
        if (component.Body is not {} body)
            return;

        if (_mobState.IsAlive(body))
        {
            args.Handled = true;
            return;
        }

        var ev = new IsRottingEvent();
        RaiseLocalEvent(body, ref ev);

        args.Handled = ev.Handled;
    }

    private void AddOrgan(
        Entity<OrganComponent> organEnt,
        EntityUid bodyUid,
        EntityUid parentPartUid)
    {
        organEnt.Comp.Body = bodyUid;
        organEnt.Comp.BodyPart = parentPartUid;

        var addedEv = new OrganAddedEvent(parentPartUid);
        RaiseLocalEvent(organEnt, ref addedEv);

        var limbEv = new OrganAddedLimbEvent(organEnt);
        RaiseLocalEvent(parentPartUid, ref limbEv);

        if (organEnt.Comp.Body is not null)
        {
            var addedInBodyEv = new OrganAddedToBodyEvent(bodyUid, parentPartUid);
            RaiseLocalEvent(organEnt, ref addedInBodyEv);

            var bodyAddedEv = new OrganAddedBodyEvent(organEnt);
            RaiseLocalEvent(bodyUid, ref bodyAddedEv);
        }

        Dirty(organEnt, organEnt.Comp);
    }

    private void RemoveOrgan(Entity<OrganComponent> organEnt, EntityUid parentPartUid)
    {
        var removedEv = new OrganRemovedEvent(parentPartUid);
        RaiseLocalEvent(organEnt, ref removedEv);

        var limbRemovedEv = new OrganRemovedLimbEvent(organEnt);
        RaiseLocalEvent(parentPartUid, ref limbRemovedEv);

        if (organEnt.Comp.Body is { Valid: true } bodyUid)
        {
            var removedInBodyEv = new OrganRemovedFromBodyEvent(bodyUid, parentPartUid);
            RaiseLocalEvent(organEnt, ref removedInBodyEv);

            var bodyRemovedEv = new OrganRemovedBodyEvent(organEnt);
            RaiseLocalEvent(bodyUid, ref bodyRemovedEv);
        }

        organEnt.Comp.Body = null;
        organEnt.Comp.BodyPart = null;
        Dirty(organEnt, organEnt.Comp);
    }

    private OrganSlot? CreateOrganSlot(Entity<OrganComponent?> organ, Entity<BodyPartComponent?> parentEnt, string slotId)
    {
        if (!Resolve(organ, ref organ.Comp))
            return null;

        if (!Resolve(parentEnt, ref parentEnt.Comp, logMissing: false))
            return null;

        var container = Containers.EnsureContainer<ContainerSlot>(parentEnt, GetOrganContainerId(slotId));
        if (!Containers.Insert(organ.Owner, container))
            return null;

        var slot = new OrganSlot(slotId, organ.Comp.OrganType);
        parentEnt.Comp.Organs.Add(slotId, slot);
        return slot;
    }


    /// <summary>
    /// Returns whether the slotId exists on the partId.
    /// </summary>
    public bool CanInsertOrgan(
        EntityUid partId,
        string slotId,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part) && part.Organs.ContainsKey(slotId);
    }

    /// <summary>
    /// Returns whether the specified organ slot exists on the partId.
    /// </summary>
    public bool CanInsertOrgan(
        EntityUid partId,
        OrganSlot slot,
        BodyPartComponent? part = null)
    {
        return CanInsertOrgan(partId, slot.Id, part);
    }

    public bool InsertOrgan(
        EntityUid partId,
        EntityUid organId,
        string slotId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ, logMissing: false)
            || !Resolve(partId, ref part, logMissing: false)
            || !CanInsertOrgan(partId, slotId, part))
        {
            return false;
        }

        organ.BodyPart = partId;
        Dirty(organId, organ);

        var containerId = GetOrganContainerId(slotId);

        return Containers.TryGetContainer(partId, containerId, out var container)
            && Containers.Insert(organId, container);
    }

    /// <summary>
    /// Removes the organ if it is inside of a body part.
    /// </summary>
    public bool RemoveOrgan(EntityUid organId, OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ))
            return false;

        if (!Containers.TryGetContainingContainer((organId, null, null), out var container))
            return false;

        var parent = container.Owner;
        organ.BodyPart = null;
        Dirty(organId, organ);

        return HasComp<BodyPartComponent>(parent)
            && Containers.Remove(organId, container);
    }

    /// <summary>
    /// Tries to add this organ to any matching slot on this body part.
    /// </summary>
    public bool AddOrganToFirstValidSlot(
        EntityUid partId,
        EntityUid organId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!Resolve(partId, ref part, logMissing: false)
            || !Resolve(organId, ref organ, logMissing: false))
        {
            return false;
        }

        foreach (var (slotId, organSlot) in part.Organs)
        {
            if (organSlot.OrganType != organ.OrganType)
                continue;

            InsertOrgan(partId, organId, slotId, part, organ);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a list of Entity<<see cref="T"/>, <see cref="OrganComponent"/>>
    /// for each organ of the body
    /// </summary>
    /// <typeparam name="T">The component that we want to return</typeparam>
    /// <param name="entity">The body to check the organs of</param>
    public List<Entity<T, OrganComponent>> GetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity)
        where T : IComponent
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<Entity<T, OrganComponent>>();

        var query = GetEntityQuery<T>();
        var list = new List<Entity<T, OrganComponent>>(3);
        foreach (var organ in GetBodyOrgans(entity.Owner, entity.Comp))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((organ.Id, comp, organ.Component));
        }

        return list;
    }

    /// <summary>
    ///     Tries to get a list of ValueTuples of <see cref="T"/> and OrganComponent on each organs
    ///     in the given body.
    /// </summary>
    /// <param name="uid">The body entity id to check on.</param>
    /// <param name="comps">The list of components.</param>
    /// <param name="body">The body to check for organs on.</param>
    /// <typeparam name="T">The component to check for.</typeparam>
    /// <returns>Whether any were found.</returns>
    public bool TryGetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity,
        [NotNullWhen(true)] out List<Entity<T, OrganComponent>>? comps)
        where T : IComponent
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
        {
            comps = null;
            return false;
        }

        comps = GetBodyOrganEntityComps<T>(entity);

        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }
}
