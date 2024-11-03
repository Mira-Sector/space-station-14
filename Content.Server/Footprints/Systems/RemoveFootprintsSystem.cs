using Content.Server.Footprints.Components;
using Content.Shared.Foldable;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Interaction;

namespace Content.Server.Footprint.Systems;

public sealed class RemoveFootprintsSystem : EntitySystem
{
    [Dependency] private readonly FootprintSystem _footprint = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RemoveFootprintsComponent, StepTriggeredOffEvent>(OnStep);
        SubscribeLocalEvent<RemoveFootprintsComponent, StepTriggerAttemptEvent>(OnStepAttempt);

        SubscribeLocalEvent<RemoveFootprintsComponent, FoldedEvent>(OnFold);
        SubscribeLocalEvent<RemoveFootprintsComponent, InteractHandEvent>(OnInteract);
    }

    private void OnStep(EntityUid uid, RemoveFootprintsComponent component, ref StepTriggeredOffEvent args)
    {
        if (!component.Enabled)
            return;

        if (HasComp<FootprintComponent>(args.Tripper))
        {
            EntityManager.QueueDeleteEntity(args.Tripper);
            return;
        }

        var messMaker = _footprint.GetMessMaker(args.Tripper);

        if (messMaker == EntityUid.Invalid)
            return;

        RemComp<CanLeaveFootprintsComponent>(messMaker);
    }

    private void OnStepAttempt(EntityUid uid, RemoveFootprintsComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue |= component.Enabled;
    }

    private void OnFold(EntityUid uid, RemoveFootprintsComponent component, ref FoldedEvent args)
    {
        component.Enabled = !args.IsFolded;
    }

    private void OnInteract(EntityUid uid, RemoveFootprintsComponent component, InteractHandEvent args)
    {
        args.Handled |= component.Enabled;
    }
}
