namespace Content.Shared.Atmos.Piping.Crawling.Systems;

public abstract partial class SharedPipeCrawlingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeEntry();
    }
}
