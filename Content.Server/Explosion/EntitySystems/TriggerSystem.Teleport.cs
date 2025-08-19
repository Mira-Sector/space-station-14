
using Content.Shared.Coordinates;
using Content.Shared.Explosion.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using System.Numerics;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        private EntityQuery<PhysicsComponent> _physicsQuery; // declare the variable for the query

        private void InitializeTeleport()
        {
            SubscribeLocalEvent<TeleportOnTriggerComponent, TriggerEvent>(OnTeleportTrigger);

            _physicsQuery = GetEntityQuery<PhysicsComponent>();
        }
        private void OnTeleportTrigger(EntityUid uid, TeleportOnTriggerComponent component, TriggerEvent args)
        {
            if (component.TeleportFrom == null)
                component.TeleportFrom = uid;
            if (component.TeleportTo == null)
                component.TeleportTo = component.TeleportFrom;

            var tpFrom = component.TeleportFrom ?? uid;
            var tpTo = component.TeleportTo ?? uid;

            Log.Debug($"from: {tpFrom.Id.ToString()}");
            Log.Debug($"to: {tpTo.Id.ToString()}");

            var entities = _lookup.GetEntitiesInRange(tpFrom, component.TeleportRadius, flags: LookupFlags.Uncontained);
            foreach (var ent in entities)
            {
                if (!_physicsQuery.HasComp(ent))
                    continue;

                var tpEnt = Transform(ent);

                if (tpEnt.Anchored == true)
                    continue;

                //var pos = _transformSystem.ToMapCoordinates(tpEnt.Coordinates).Position;

                _transformSystem.DropNextTo(ent, tpTo);
                var randomX = _transformSystem.ToMapCoordinates(ent.ToCoordinates()).X + _random.NextFloat(-component.TeleportScatterRange, component.TeleportScatterRange);
                var randomY = _transformSystem.ToMapCoordinates(ent.ToCoordinates()).Y + _random.NextFloat(-component.TeleportScatterRange, component.TeleportScatterRange);

                var scatterpos = new Vector2(randomX, randomY);

                //_adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent)} has been teleported to {scatterpos} by teleporter {ToPrettyString(uid)}");

                _transformSystem.SetWorldPosition(ent, scatterpos);
                //roll for teleport incident
            }
            //raise teleport event
        }
    }

}