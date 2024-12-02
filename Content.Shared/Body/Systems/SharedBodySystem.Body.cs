using System.Linq;
using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DragDrop;
using Content.Shared.Gibbing.Components;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Inventory;
using Content.Shared.Radiation.Events;
using Content.Shared.Rejuvenate;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    /*
     * tl;dr of how bobby works
     * - BodyComponent uses a BodyPrototype as a template.
     * - On MapInit we spawn the root entity in the prototype and spawn all connections outwards from here
     * - Each "connection" is a body part (e.g. arm, hand, etc.) and each part can also contain organs.
     */

    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private const float GibletLaunchImpulse = 8;
    private const float GibletLaunchImpulseVariance = 3;

    private void InitializeBody()
    {
        // Body here to handle root body parts.
        SubscribeLocalEvent<BodyComponent, EntInsertedIntoContainerMessage>(OnBodyInserted);
        SubscribeLocalEvent<BodyComponent, EntRemovedFromContainerMessage>(OnBodyRemoved);

        SubscribeLocalEvent<BodyComponent, ComponentInit>(OnBodyInit);
        SubscribeLocalEvent<BodyComponent, ComponentRemove>(OnBodyRemove);
        SubscribeLocalEvent<BodyComponent, MapInitEvent>(OnBodyMapInit);
        SubscribeLocalEvent<BodyComponent, CanDragEvent>(OnBodyCanDrag);
        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<BodyComponent, OnIrradiatedEvent>(OnIrradiatedEvent);

        SubscribeLocalEvent<BodyPartComponent, ComponentStartup>(OnPartStartup);
        SubscribeLocalEvent<BodyPartComponent, DamageModifyEvent>(RelayToBody);
        SubscribeLocalEvent<BodyPartComponent, DamageChangedEvent>(RelayToBody);
    }

    private void OnBodyInserted(Entity<BodyComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // Root body part?
        var slotId = args.Container.ID;

        if (slotId != BodyRootContainerId)
            return;

        var insertedUid = args.Entity;

        if (TryComp(insertedUid, out BodyPartComponent? part))
        {
            AddPart((ent, ent), (insertedUid, part), slotId);
            RecursiveBodyUpdate((insertedUid, part), ent);
        }

        if (TryComp(insertedUid, out OrganComponent? organ))
        {
            AddOrgan((insertedUid, organ), ent, ent);
        }
    }

    private void OnBodyRemoved(Entity<BodyComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Root body part?
        var slotId = args.Container.ID;

        if (slotId != BodyRootContainerId)
            return;

        var removedUid = args.Entity;
        DebugTools.Assert(!TryComp(removedUid, out BodyPartComponent? b) || b.Body == ent);
        DebugTools.Assert(!TryComp(removedUid, out OrganComponent? o) || o.Body == ent);

        if (TryComp(removedUid, out BodyPartComponent? part))
        {
            RemovePart((ent, ent), (removedUid, part), slotId);
            RecursiveBodyUpdate((removedUid, part), null);
        }

        if (TryComp(removedUid, out OrganComponent? organ))
            RemoveOrgan((removedUid, organ), ent);
    }

    private void OnBodyInit(Entity<BodyComponent> ent, ref ComponentInit args)
    {
        // Setup the initial container.
        ent.Comp.RootContainer = Containers.EnsureContainer<ContainerSlot>(ent, BodyRootContainerId);
        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnBodyRemove(Entity<BodyComponent> ent, ref ComponentRemove args)
    {
        _alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    private void OnBodyMapInit(EntityUid uid, BodyComponent component, MapInitEvent args)
    {
        if (component.Prototype is null)
            return;

        // One-time setup
        // Obviously can't run in Init to avoid double-spawns on save / load.
        var prototype = Prototypes.Index(component.Prototype.Value);
        MapInitBody(uid, prototype);
    }

    private void MapInitBody(EntityUid bodyEntity, BodyPrototype prototype)
    {
        var protoRoot = prototype.Slots[prototype.Root];
        if (protoRoot.Part is null)
            return;

        // This should already handle adding the entity to the root.
        var rootPartUid = SpawnInContainerOrDrop(protoRoot.Part, bodyEntity, BodyRootContainerId);
        var rootPart = Comp<BodyPartComponent>(rootPartUid);
        rootPart.Body = bodyEntity;
        Dirty(rootPartUid, rootPart);

        // Setup the rest of the body entities.
        SetupOrgans((rootPartUid, rootPart), protoRoot.Organs);
        MapInitParts(rootPartUid, prototype);
    }

    private void OnBodyCanDrag(Entity<BodyComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnRejuvenate(EntityUid uid, BodyComponent component, RejuvenateEvent args)
    {
        var parts = GetBodyDamageable(uid, component);

        foreach ((var partUid, var partDamageable) in parts)
        {
            RaiseLocalEvent(partUid, new RejuvenateEvent());
        }
    }
    private void OnIrradiatedEvent(EntityUid uid, BodyComponent component, OnIrradiatedEvent args)
    {
        var damageable = GetBodyDamageable(uid, component).Values;

        var mostFrequent = damageable
            .GroupBy(item => item.RadiationDamageTypeIDs)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        if (mostFrequent == null || !mostFrequent.TryFirstOrDefault(out var radiation))
            return;

        _damageable.Irradiate(uid, args.RadsPerSecond, radiation.RadiationDamageTypeIDs);
    }

    private void OnPartStartup(EntityUid uid, BodyPartComponent component, ComponentStartup args)
    {
        if (component.Body is not {} body)
            return;

        if (!TryComp<BodyComponent>(body, out var bodyComp))
            return;

        if (bodyComp.RootContainer.ContainedEntity != uid)
            return;

        var ev = new BodySetupEvent(bodyComp);
        RaiseLocalEvent(body, ev);
    }

    protected void RelayToBody<T>(EntityUid uid, BodyPartComponent component, T args) where T : class
    {
        if (component.Body == null)
            return;

        var ev = new LimbBodyRelayedEvent<T>(args, uid);

        RaiseLocalEvent(component.Body.Value, ref ev);
    }

    protected void RelayRefToBody<T>(EntityUid uid, BodyPartComponent component, ref T args) where T : class
    {
        if (component.Body == null)
            return;

        var ev = new LimbBodyRelayedEvent<T>(args, uid);

        RaiseLocalEvent(component.Body.Value, ref ev);
    }

    /// <summary>
    /// Sets up all of the relevant body parts for a particular body entity and root part.
    /// </summary>
    private void MapInitParts(EntityUid rootPartId, BodyPrototype prototype)
    {
        // Start at the root part and traverse the body graph, setting up parts as we go.
        // Basic BFS pathfind.
        var rootSlot = prototype.Root;
        var frontier = new Queue<string>();
        frontier.Enqueue(rootSlot);

        // Child -> Parent connection.
        var cameFrom = new Dictionary<string, string>();
        cameFrom[rootSlot] = rootSlot;
        // Maps slot to its relevant entity.
        var cameFromEntities = new Dictionary<string, EntityUid>();
        cameFromEntities[rootSlot] = rootPartId;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                // Already been handled
                if (!cameFrom.TryAdd(connection, currentSlotId))
                    continue;

                // Setup part
                var connectionSlot = prototype.Slots[connection];
                var parentEntity = cameFromEntities[currentSlotId];
                var parentPartComponent = Comp<BodyPartComponent>(parentEntity);

                // Spawn the entity on the target
                // then get the body part type, create the slot, and finally
                // we can insert it into the container.
                var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                cameFromEntities[connection] = childPart;

                var childPartComponent = Comp<BodyPartComponent>(childPart);
                var partSlot = CreatePartSlot(parentEntity, connection, childPartComponent.PartType, parentPartComponent);
                var cont = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));

                if (partSlot is null || !Containers.Insert(childPart, cont))
                {
                    Log.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                    QueueDel(childPart);
                    continue;
                }

                // Add organs
                SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);

                // Enqueue it so we can also get its neighbors.
                frontier.Enqueue(connection);
            }
        }
    }

    private void SetupOrgans(Entity<BodyPartComponent> ent, Dictionary<string, string> organs)
    {
        foreach (var (organSlotId, organProto) in organs)
        {
            var slot = CreateOrganSlot((ent, ent), organSlotId);
            SpawnInContainerOrDrop(organProto, ent, GetOrganContainerId(organSlotId));

            if (slot is null)
            {
                Log.Error($"Could not create organ for slot {organSlotId} in {ToPrettyString(ent)}");
            }
        }
    }

    /// <summary>
    /// Gets all body containers on this entity including the root one.
    /// </summary>
    public IEnumerable<BaseContainer> GetBodyContainers(
        EntityUid id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (!Resolve(id, ref body, logMissing: false)
            || body.RootContainer.ContainedEntity is null
            || !Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
        {
            yield break;
        }

        yield return body.RootContainer;

        foreach (var childContainer in GetPartContainers(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return childContainer;
        }
    }

    /// <summary>
    /// Gets all child body parts of this entity, including the root entity.
    /// </summary>
    public IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(
        EntityUid? id,
        BodyComponent? body = null,
        BodyPartComponent? rootPart = null)
    {
        if (id is null)
            yield break;

        if (!Resolve(id.Value, ref body, logMissing: false))
            yield break;

        if (body.RootContainer.ContainedEntity is null)
            yield break;

        if (!Resolve(body.RootContainer.ContainedEntity.Value, ref rootPart))
            yield break;

        foreach (var child in GetBodyPartChildren(body.RootContainer.ContainedEntity.Value, rootPart))
        {
            yield return child;
        }
    }

    public IEnumerable<(EntityUid Id, OrganComponent Component)> GetBodyOrgans(
        EntityUid? bodyId,
        BodyComponent? body = null)
    {
        if (bodyId is null || !Resolve(bodyId.Value, ref body, logMissing: false))
            yield break;

        foreach (var part in GetBodyChildren(bodyId, body))
        {
            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                yield return organ;
            }
        }
    }

    /// <summary>
    /// Returns all body part slots for this entity.
    /// </summary>
    /// <param name="bodyId"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public IEnumerable<BodyPartSlot> GetBodyAllSlots(
        EntityUid bodyId,
        BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, logMissing: false)
            || body.RootContainer.ContainedEntity is null)
        {
            yield break;
        }

        foreach (var slot in GetAllBodyPartSlots(body.RootContainer.ContainedEntity.Value))
        {
            yield return slot;
        }
    }

    public DamageSpecifier? GetBodyDamage(
        EntityUid bodyId,
        BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, false))
        {
            return null;
        }

        DamageSpecifier totalDamage = new();

        foreach (var (partUid, partComp) in GetBodyChildren(bodyId, body))
        {
            if (!TryComp<DamageableComponent>(partUid, out var partDamageComp))
                continue;

            totalDamage += partDamageComp.Damage * partComp.OverallDamageScale;
        }

        return totalDamage;
    }

    public Dictionary<EntityUid, DamageableComponent> GetBodyDamageable(
        EntityUid? bodyId,
        BodyComponent? body = null)
    {
        Dictionary<EntityUid, DamageableComponent> damageableComps = new ();

        if (bodyId == null || !Resolve(bodyId.Value, ref body, false))
            return damageableComps;

        foreach (var (part, _) in GetBodyChildren(bodyId, body))
        {
            if (!TryComp<DamageableComponent>(part, out var partDamageComp))
                continue;

            damageableComps.Add(part, partDamageComp);
        }

        return damageableComps;
    }

    public ProtoId<DamageContainerPrototype>? GetMostFrequentDamageContainer(
        EntityUid bodyId,
        BodyComponent? body = null)
    {
        if (!Resolve(bodyId, ref body, false))
        {
            return null;
        }

        List<ProtoId<DamageContainerPrototype>?> damageContainers = new ();

        foreach (var (part, _) in GetBodyChildren(bodyId, body))
        {
            if (!TryComp<DamageableComponent>(part, out var partDamageComp))
                continue;

            damageContainers.Add(partDamageComp.DamageContainerID);
        }

        return GetMostFrequentDamageContainer(damageContainers);
    }

    public ProtoId<DamageContainerPrototype>? GetMostFrequentDamageContainer(IEnumerable<ProtoId<DamageContainerPrototype>?> data)
    {
        if (data == null || !data.Any())
        {
            throw new ArgumentException($"{data} is null or empty.");
        }

        var mostFrequent = data
            .GroupBy(item => item)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        return mostFrequent != null
            ? mostFrequent.Key
            : throw new InvalidOperationException($"Unexpected error during processing {data}.");
    }

    public virtual HashSet<EntityUid> GibBody(
        EntityUid bodyId,
        bool gibOrgans = false,
        BodyComponent? body = null,
        bool launchGibs = true,
        Vector2? splatDirection = null,
        float splatModifier = 1,
        Angle splatCone = default,
        SoundSpecifier? gibSoundOverride = null)
    {
        var gibs = new HashSet<EntityUid>();

        if (!Resolve(bodyId, ref body, logMissing: false))
            return gibs;

        var root = GetRootPartOrNull(bodyId, body);
        if (root != null && TryComp(root.Value.Entity, out GibbableComponent? gibbable))
        {
            gibSoundOverride ??= gibbable.GibSound;
        }
        var parts = GetBodyChildren(bodyId, body).ToArray();
        gibs.EnsureCapacity(parts.Length);
        foreach (var part in parts)
        {

            _gibbingSystem.TryGibEntityWithRef(bodyId, part.Id, GibType.Gib, GibContentsOption.Skip, ref gibs,
                playAudio: false, launchGibs:true, launchDirection:splatDirection, launchImpulse: GibletLaunchImpulse * splatModifier,
                launchImpulseVariance:GibletLaunchImpulseVariance, launchCone: splatCone);

            if (!gibOrgans)
                continue;

            foreach (var organ in GetPartOrgans(part.Id, part.Component))
            {
                _gibbingSystem.TryGibEntityWithRef(bodyId, organ.Id, GibType.Drop, GibContentsOption.Skip,
                    ref gibs, playAudio: false, launchImpulse: GibletLaunchImpulse * splatModifier,
                    launchImpulseVariance:GibletLaunchImpulseVariance, launchCone: splatCone);
            }
        }

        var bodyTransform = Transform(bodyId);
        if (TryComp<InventoryComponent>(bodyId, out var inventory))
        {
            foreach (var item in _inventory.GetHandOrInventoryEntities(bodyId))
            {
                SharedTransform.DropNextTo(item, (bodyId, bodyTransform));
                gibs.Add(item);
            }
        }
        _audioSystem.PlayPredicted(gibSoundOverride, bodyTransform.Coordinates, null);
        return gibs;
    }
}
