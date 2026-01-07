using Content.Server.Footprints.Components;
using Content.Shared.Foldable;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Interaction;

namespace Content.Server.Footprints.Systems;

public sealed partial class FootprintSystem : EntitySystem
{
    private void InitializeRemove()
    {
        SubscribeLocalEvent<RemoveFootprintsComponent, StepTriggeredOffEvent>(OnStep);
        SubscribeLocalEvent<RemoveFootprintsComponent, StepTriggerAttemptEvent>(OnStepAttempt);

        SubscribeLocalEvent<RemoveFootprintsComponent, FoldedEvent>(OnFold);
        SubscribeLocalEvent<RemoveFootprintsComponent, InteractHandEvent>(OnInteract);
    }

    private void OnStep(Entity<RemoveFootprintsComponent> ent, ref StepTriggeredOffEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        if (HasComp<FootprintComponent>(args.Tripper))
        {
            EntityManager.QueueDeleteEntity(args.Tripper);
            return;
        }

        var messMaker = GetMessMaker(args.Tripper);

        if (messMaker == EntityUid.Invalid)
            return;

        RemComp<CanLeaveFootprintsComponent>(messMaker);
    }

    private void OnStepAttempt(Entity<RemoveFootprintsComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue |= ent.Comp.Enabled;
    }

    private void OnFold(Entity<RemoveFootprintsComponent> ent, ref FoldedEvent args)
    {
        ent.Comp.Enabled = !args.IsFolded;
    }

    private void OnInteract(Entity<RemoveFootprintsComponent> ent, ref InteractHandEvent args)
    {
        args.Handled |= ent.Comp.Enabled;
    }
}
