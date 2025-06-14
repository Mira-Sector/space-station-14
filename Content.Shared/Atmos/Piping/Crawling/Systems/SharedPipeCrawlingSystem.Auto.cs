using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Crawling.Components;

namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public partial class SharedPipeCrawlingSystem
{
    private void UpdateAuto(Entity<PipeCrawlingAutoPilotComponent, PipeCrawlingComponent, CanEnterPipeCrawlingComponent> ent)
    {
        var pipe = PipeQuery.Comp(ent.Comp2.CurrentPipe);
        if (!TryGetNextPipe((ent.Comp2.CurrentPipe, pipe), ent.Comp2.CurrentLayer, ent.Comp2.Direction, out var nextPipe))
        {
            RemCompDeferred<PipeCrawlingAutoPilotComponent>(ent.Owner);
            return;
        }

        TransferPipe((ent.Owner, ent.Comp2), nextPipe.Value);

        if (nextPipe.Value.Owner != ent.Comp1.TargetPipe)
            return;

        // destination reached
        RemCompDeferred<PipeCrawlingAutoPilotComponent>(ent.Owner);
    }

    private Entity<PipeCrawlingPipeComponent> GetNextStop(Entity<PipeCrawlingPipeComponent> start, AtmosPipeLayer layer, Direction direction)
    {
        var oppositeDir = direction.GetOpposite();

        var currentPipe = start;
        while (TryGetNextPipe(currentPipe, layer, direction, out var nextPipe))
        {
            // player may want to get off my wild ride
            if (EnterQuery.HasComp(nextPipe))
                return nextPipe.Value;

            // may want to change layers
            if (nextPipe.Value.Comp.ConnectedPipes.Keys.Count > 1)
                return nextPipe.Value;

            // safe to index as the while loop checks this
            foreach (var (connectedDirection, _) in nextPipe.Value.Comp.ConnectedPipes[layer])
            {
                if (connectedDirection == direction)
                    continue;

                if (connectedDirection == oppositeDir)
                    continue;

                // we foun a new fork in the way
                // allow the player to make a choice
                return nextPipe.Value;
            }

            currentPipe = nextPipe.Value;
        }

        return currentPipe;
    }

}
