namespace Content.Shared.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeBody();
    }
}
