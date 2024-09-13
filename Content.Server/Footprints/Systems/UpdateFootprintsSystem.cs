using Content.Server.Footprints.Components;
using Content.Shared.Fluids;
using Robust.Shared.Physics.Events;

namespace Content.Server.Footprint.Systems;

public sealed class UpdateFootprintystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GivesFootprintsComponent, EndCollideEvent>(OnStep);
    }

    private void OnStep(EntityUid uid, GivesFootprintsComponent component, ref EndCollideEvent args)
    {
        if (!TryComp<LeavesFootprintsComponent>(args.OtherEntity, out var footprintComp))
            return;

        var playerFootprintComp = EnsureComp<CanLeaveFootprintsComponent>(args.OtherEntity);

        var color = playerFootprintComp.Color;

        if (_appearance.TryGetData<Color>(uid, PuddleVisuals.SolutionColor, out color))
            color *= playerFootprintComp.Color;

        playerFootprintComp.LastFootstep = _transform.GetMapCoordinates(args.OtherEntity);
        playerFootprintComp.FootstepsLeft = footprintComp.MaxFootsteps;
        playerFootprintComp.Color = color;
    }
}
