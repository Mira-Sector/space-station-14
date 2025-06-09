namespace Content.Shared.Modules.ModSuit;

public abstract partial class SharedModSuitSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeComplexity();
        InitializeDeployable();
        InitializeSealable();
        InitializeUI();
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSealable(frameTime);
    }
}
