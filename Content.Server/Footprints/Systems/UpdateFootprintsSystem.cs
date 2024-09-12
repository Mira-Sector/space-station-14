using Content.Server.Footprints.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Footprint.Systems;

public sealed class UpdateFootprintystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GivesFootprintsComponent, EndCollideEvent>(OnStep);
    }

    private void OnStep(EntityUid uid, GivesFootprintsComponent component, ref EndCollideEvent args)
    {
        if (!TryComp<LeavesFootprintsComponent>(args.OtherEntity, out var footprintComp))
            return;

        Color color = Color.White;

        var playerFootprintComp = EnsureComp<CanLeaveFootprintsComponent>(args.OtherEntity);

        playerFootprintComp.LastFootstep = _transform.GetMapCoordinates(args.OtherEntity);
        playerFootprintComp.FootstepsLeft = footprintComp.MaxFootsteps;
        playerFootprintComp.Color = color;
    }
}
