using Content.Server.Forensics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Stains;

namespace Content.Server.Stains;

public sealed partial class StainableSystem : SharedStainableSystem
{
    [Dependency] private readonly ForensicsSystem _forensics = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void WashingForensics(Entity<StainableComponent> ent, Entity<SolutionComponent> solution, EntityUid washingMachine)
    {
        if (!TryComp<ForensicsComponent>(washingMachine, out var forensics))
            return;

        forensics.DNAs.UnionWith(_forensics.GetSolutionsDNA(solution.Comp.Solution));
    }
}
