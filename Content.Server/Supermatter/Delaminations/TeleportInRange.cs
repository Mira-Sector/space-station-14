using Content.Server.Supermatter.Components;
using Content.Server.Supermatter.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Supermatter.Delaminations;

[DataDefinition]
public sealed partial class TeleportInRange : SupermatterDelaminationType
{
    [DataField]
    public float Range;

    [DataField]
    public EntityWhitelist Whitelist = new();

    public override void Delaminate(EntityUid supermatter, IEntityManager entMan)
    {
        var lookupSys = entMan.System<EntityLookupSystem>();
        var physicsSys = entMan.System<SharedPhysicsSystem>();
        var transformSys = entMan.System<SharedTransformSystem>();
        var whitelistSys = entMan.System<EntityWhitelistSystem>();

        Dictionary<EntityUid, MapCoordinates> entities = new();

        foreach (var entity in lookupSys.GetEntitiesInRange(supermatter, Range))
        {
            if (whitelistSys.IsWhitelistPass(Whitelist, entity))
                entities.Add(entity, transformSys.GetMapCoordinates(entity));
        }

        var startingCoords = new Dictionary<EntityUid, MapCoordinates>(entities);

        var ev = new SupermatterDelaminationTeleportGetPositionEvent(entities);
        entMan.EventBus.RaiseLocalEvent(supermatter, ev, true);

        if (!ev.Handled)
            return;

        foreach (var (entity, pos) in ev.Entities)
        {
            transformSys.SetMapCoordinates(entity, pos);
            entMan.EnsureComponent<SupermatterDelaminationTeleportedComponent>(entity).StartingCoords = startingCoords[entity];

            if (entMan.TryGetComponent<PhysicsComponent>(entity, out var physics))
                physicsSys.ResetDynamics(entity, physics);
        }
    }
}
