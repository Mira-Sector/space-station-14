using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    private void InitializeComplexity()
    {
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleAddedEvent>(OnComplexityModuleAdded);
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleRemovedEvent>(OnComplexityModuleRemoved);
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleAddingAttemptEvent>(OnComplexityModuleAttempt);

        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModSuitGetUiStatesEvent>(OnGetModSuitUiState);
    }

    private void OnComplexityModuleAdded(Entity<ModSuitComplexityLimitComponent> ent, ref ModuleContainerModuleAddedEvent args)
    {
        if (!TryComp<ModSuitModuleComplexityComponent>(args.Module, out var moduleComp))
            return;

        ent.Comp.Complexity += moduleComp.Complexity;
        UpdateUI(ent.Owner);
    }

    private void OnComplexityModuleRemoved(Entity<ModSuitComplexityLimitComponent> ent, ref ModuleContainerModuleRemovedEvent args)
    {
        if (!TryComp<ModSuitModuleComplexityComponent>(args.Module, out var moduleComp))
            return;

        ent.Comp.Complexity -= moduleComp.Complexity;
        UpdateUI(ent.Owner);
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

    private void OnGetModSuitUiState(Entity<ModSuitComplexityLimitComponent> ent, ref ModSuitGetUiStatesEvent args)
    {
        var toAdd = (ent.Comp.Complexity, ent.Comp.MaxComplexity);
        ModSuitComplexityBoundUserInterfaceState? foundState = null;

        foreach (var state in args.States)
        {
            if (state is not ModSuitComplexityBoundUserInterfaceState complexityState)
                continue;

            foundState = complexityState;
            break;
        }

        if (foundState == null)
        {
            var newState = new ModSuitComplexityBoundUserInterfaceState(toAdd);
            args.States.Add(newState);
            return;
        }

        foundState.Complexity = toAdd;
    }
}
