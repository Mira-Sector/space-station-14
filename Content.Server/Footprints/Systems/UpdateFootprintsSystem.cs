using Content.Shared.StepTrigger.Systems;
using Content.Server.Footprints.Components;


namespace Content.Server.Footprint.Systems;

public sealed class UpdateFootprintystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GivesFootprintsComponent, StepTriggerAttemptEvent>(OnStepAttempt);
        SubscribeLocalEvent<GivesFootprintsComponent, StepTriggeredOffEvent>(OnStep);
    }

    private void OnStep(EntityUid uid, GivesFootprintsComponent component, ref StepTriggeredOffEvent args)
    {
        if (!TryComp<LeavesFootprintsComponent>(args.Tripper, out var footprintComp))
            return;

        Color color = Color.White;

        var playerFootprintComp = EnsureComp<CanLeaveFootprintsComponent>(args.Tripper);

        playerFootprintComp.LastFootstep = _transform.GetMapCoordinates(args.Tripper);
        playerFootprintComp.FootstepsLeft = footprintComp.MaxFootsteps;
        playerFootprintComp.Color = color;
    }

    private static void OnStepAttempt(EntityUid uid, GivesFootprintsComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }
}
