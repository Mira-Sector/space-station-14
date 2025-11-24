using Content.Shared.Arcade.Racer.Objects;
using Content.Shared.Arcade.Racer.Objects.Vehicles;
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
        var startingNode = startingStage.Graph.Nodes[startingStage.Graph.StartingNode!];

        List<BaseRacerGameObject> objects = new(1);
        var player = new RacerGameVehiclePlayer()
        {
            Position = startingNode.Position
        };
        objects.Add(player);

        ent.Comp.State = new()
        {
            CurrentStage = ent.Comp.StartingStage,
            CurrentNode = startingNode,
            Objects = objects,
            Player = player
        };
    }
}
