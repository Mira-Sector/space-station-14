using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class SpawnItemsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnItemsOnUseComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<SpawnItemsOnUseComponent, PriceCalculationEvent>(CalculatePrice, before: new[] { typeof(PricingSystem) });

            SubscribeLocalEvent<SpawnItemsOnLandComponent, LandEvent>(OnLand);
            SubscribeLocalEvent<SpawnItemsOnLandComponent, PriceCalculationEvent>(CalculatePrice, before: new[] { typeof(PricingSystem) });
        }

        private void CalculatePrice(EntityUid uid, ISpawnItems component, ref PriceCalculationEvent args)
        {
            var ungrouped = CollectOrGroups(component.Items, out var orGroups);

            foreach (var entry in ungrouped)
            {
                var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                // Calculate the average price of the possible spawned items
                args.Price += _pricing.GetPrice(protUid) * entry.SpawnProbability * entry.GetAmount(getAverage: true);

                Del(protUid);
            }

            foreach (var group in orGroups)
            {
                foreach (var entry in group.Entries)
                {
                    var protUid = Spawn(entry.PrototypeId, MapCoordinates.Nullspace);

                    // Calculate the average price of the possible spawned items
                    args.Price += _pricing.GetPrice(protUid) *
                                  (entry.SpawnProbability / group.CumulativeProbability) *
                                  entry.GetAmount(getAverage: true);

                    Del(protUid);
                }
            }

            args.Handled = true;
        }

        private void OnUseInHand(EntityUid uid, ISpawnItems component, UseInHandEvent args)
        {
            if (args.Handled)
                return;

            var entity = SpawnItems(uid, component, Transform(args.User).Coordinates);
            if (entity == null)
                return;

            args.Handled = true;
            _hands.PickupOrDrop(args.User, entity.Value);

        }

        private void OnLand(EntityUid uid, ISpawnItems component, LandEvent args)
        {
            SpawnItems(uid, component, Transform(uid).Coordinates);
        }

        private EntityUid? SpawnItems(EntityUid uid, ISpawnItems component, EntityCoordinates coords)
        {
            // If starting with zero or less uses, this component is a no-op
            if (component.Uses <= 0)
                return null;

            var spawnEntities = GetSpawns(component.Items, _random);
            EntityUid? entityToPlaceInHands = null;

            foreach (var proto in spawnEntities)
            {
                entityToPlaceInHands = Spawn(proto, coords);
            }

            // The entity is often deleted, so play the sound at its position rather than parenting
            if (component.Sound != null)
                _audio.PlayPvs(component.Sound, coords);

            component.Uses--;

            // Delete entity only if component was successfully used
            if (component.Uses <= 0)
            {
                // Don't delete the entity in the event bus, so we queue it for deletion.
                // We need the free hand for the new item, so we send it to nullspace.
                _transform.DetachEntity(uid, Transform(uid));
                QueueDel(uid);
            }

            return entityToPlaceInHands;
        }
    }
}
