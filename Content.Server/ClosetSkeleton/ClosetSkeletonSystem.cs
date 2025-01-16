using Content.Shared.Station;
using Content.Server.StationEvents.Components;

namespace Content.Server.ClosetSkeleton;

public sealed partial class ClosetSkeletonSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSpawningSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClosetSkeletonComponent, RandomEntityStorageSpawnedEvent>(OnSpawned);
    }

    private void OnSpawned(EntityUid uid, ClosetSkeletonComponent component, RandomEntityStorageSpawnedEvent args)
    {
        if (!TryComp<ClosetSkeletonLockerComponent>(args.Storage, out var lockerComp))
        {
            _station.EquipStartingGear(uid, component.FallbackEquipment);
            return;
        }

        _station.EquipStartingGear(uid, lockerComp.Equipment);
    }
}
