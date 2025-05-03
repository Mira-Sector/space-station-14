using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;

namespace Content.Shared.Modules.ModSuit;

public sealed partial class ModSuitSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleAddedEvent>(OnComplexityModuleAdded);
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleRemovedEvent>(OnComplexityModuleRemoved);
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleAddingAttemptEvent>(OnComplexityModuleAttempt);
    }

    #region Complexity

    private void OnComplexityModuleAdded(Entity<ModSuitComplexityLimitComponent> ent, ref ModuleContainerModuleAddedEvent args)
    {
        if (!TryComp<ModSuitModuleComplexityComponent>(args.Module, out var moduleComp))
            return;

        ent.Comp.Complexity += moduleComp.Complexity;
    }

    private void OnComplexityModuleRemoved(Entity<ModSuitComplexityLimitComponent> ent, ref ModuleContainerModuleRemovedEvent args)
    {
        if (!TryComp<ModSuitModuleComplexityComponent>(args.Module, out var moduleComp))
            return;

        ent.Comp.Complexity -= moduleComp.Complexity;
    }

    private void OnComplexityModuleAttempt(Entity<ModSuitComplexityLimitComponent> ent, ref ModuleContainerModuleAddingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<ModSuitModuleComplexityComponent>(args.Module, out var moduleComp))
            return;

        if (ent.Comp.Complexity + moduleComp.Complexity > ent.Comp.MaxComplexity)
            args.Cancel();
    }

    #endregion
}
