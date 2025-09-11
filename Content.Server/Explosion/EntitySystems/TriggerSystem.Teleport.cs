
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Explosion.Components;
using Content.Shared.Magic.Events;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Physics.Components;
using System.Numerics;


namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        private EntityQuery<PhysicsComponent> _physicsQuery; // declare the variable for the query

        private void InitializeTeleport()
        {
            SubscribeLocalEvent<TeleportOnTriggerComponent, TriggerEvent>(OnTeleportTrigger);

            _physicsQuery = GetEntityQuery<PhysicsComponent>();
        }
        private void OnTeleportTrigger(Entity<TeleportOnTriggerComponent> ent, ref TriggerEvent args)
        {
            if (ent.Comp.TeleportFrom == null) //backup for if no TeleporterFrom selecter, choose the Owner.
                ent.Comp.TeleportFrom = ent.Owner;
            if (ent.Comp.TeleportTo == null) //backup for if no Teleporter To, choose Teleport From to just teleport in place
                ent.Comp.TeleportTo = ent.Comp.TeleportFrom;

            var tpFrom = ent.Comp.TeleportFrom ?? ent.Owner; //denullable, shouldn't happen
            var tpTo = ent.Comp.TeleportTo ?? ent.Owner; //denullable, shouldn't happen

            var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.Teleporter.TeleportRadius, flags: LookupFlags.Uncontained); //get everything in teleport radius range that isn't in a container
            int entCount = 0;
            int incidentCount = 0;
            foreach (var tp in entities) //for each entity in list of detected entities
            {
                if (!_physicsQuery.HasComp(tp)) //if it hasn't got physics, skip it, it's probably not meant to be teleported.
                    continue;

                var tpEnt = Transform(tp);

                if (tpEnt.Anchored == true) //if it's anchored, skip it. We don't want to be teleporting the teleporter itself. Or the station's walls.
                    continue;

                _transformSystem.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the teleporter we first drop them next to the target THEN scatter.
                var scatterpos = new Vector2(
                    _transformSystem.ToMapCoordinates(tp.ToCoordinates()).X + _random.NextFloat(-ent.Comp.Teleporter.TeleportScatterRange, ent.Comp.Teleporter.TeleportScatterRange),
                    _transformSystem.ToMapCoordinates(tp.ToCoordinates()).Y + _random.NextFloat(-ent.Comp.Teleporter.TeleportScatterRange, ent.Comp.Teleporter.TeleportScatterRange));

                _transformSystem.SetWorldPosition(tp, scatterpos); //set final position after scatter
                RaiseLocalEvent(tp, new AfterTeleportEvent(tpTo, tpFrom)); //send that teleported entity an event to do something with

                if (_random.NextFloat(0, 1) < ent.Comp.Teleporter.IncidentChance) //roll for teleport incident
                {
                    RaiseLocalEvent(tp, new TeleportIncidentEvent(ent.Comp.Teleporter.IncidentMultiplier)); //sent a teleport incident to do something fun with
                    incidentCount += 1;
                }
                entCount += 1;
            }
            if (ent.Comp.TeleporterUid != null) //send an event back to the teleporter if there is one
                RaiseLocalEvent(ent.Comp.TeleporterUid ?? EntityUid.Invalid, new AfterTeleportEvent(tpTo, tpFrom)); //denullable it

            var target = Transform(tpTo);
            var from = Transform(tpFrom);
            _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {entCount} entities from {_transform.ToMapCoordinates(from.Coordinates)} to {_transform.ToMapCoordinates(target.Coordinates)} with {incidentCount} incidents");
        }
    }

}