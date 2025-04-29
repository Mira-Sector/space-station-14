using Content.Shared.Chemistry.Components;
using Content.Shared.Stains;

namespace Content.Server.Stains;

public sealed partial class StainableSystem : SharedStainableSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void WashingForensics(Entity<StainableComponent> ent, Entity<SolutionComponent> solution, EntityUid washingMachine)
    {
    }
}
