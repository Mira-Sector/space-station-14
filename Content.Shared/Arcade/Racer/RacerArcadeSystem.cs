namespace Content.Shared.Arcade.Racer;

public sealed partial class RacerArcadeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RacerArcadeComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RacerArcadeComponent> ent, ref ComponentInit args)
    {
        ent.Comp.State = new(ent.Comp.StartingStage);
    }
}
