
using Content.Shared.Teleportation.Systems;
using Content.Shared.Teleportation.Components;

using Content.Shared.Interaction.Events;
using Robust.Shared.Map;

namespace Content.Server.Teleportation;

public sealed class TeleporterSystem : SharedTeleporterSystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    public override void Initialize()
    {
        base.Initialize();
        Log.Debug("Teleporter Online");

        SubscribeLocalEvent<TeleporterComponent, InteractionAttemptEvent>(OnInteract);
        //GotEmaggedEvent
    }

    private void OnInteract(Entity<TeleporterComponent> ent, ref InteractionAttemptEvent args)
    {
        var tp = Transform(ent);
        if (!TrySpawnNextTo(ent.Comp.PortalProto, ent.Owner, out var firstPortal))
            return;

        var tpNew = new EntityCoordinates(ent.Owner, tp.Coordinates.X + 5, tp.Coordinates.Y + 5);
        var secondPortal = SpawnAtPosition(ent.Comp.PortalProto, tpNew);

        _link.TryLink(firstPortal.Value, secondPortal, true);
    }
}
