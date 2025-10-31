using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Racer;

public abstract partial class SharedRacerArcadeSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RacerArcadeComponent> ent, ref ComponentInit args)
    {
        var startingStage = PrototypeMan.Index(ent.Comp.StartingStage);
        ent.Comp.State = new()
        {
            CurrentStage = ent.Comp.StartingStage,
            CurrentNode = startingStage.Graph.Nodes[startingStage.Graph.StartingNode!]
        };
    }
}
