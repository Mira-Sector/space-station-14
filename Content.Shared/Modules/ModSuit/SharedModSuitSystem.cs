using Content.Shared.Clothing;
using Content.Shared.Item;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public abstract partial class SharedModSuitSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeDeployable();

        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleAddedEvent>(OnComplexityModuleAdded);
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleRemovedEvent>(OnComplexityModuleRemoved);
        SubscribeLocalEvent<ModSuitComplexityLimitComponent, ModuleContainerModuleAddingAttemptEvent>(OnComplexityModuleAttempt);

        SubscribeLocalEvent<ModSuitSealableComponent, ComponentInit>(OnSealableInit);
        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotUnequippedEvent>(OnSealableUnequipped);
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

    #region Sealing

    private void OnSealableInit(Entity<ModSuitSealableComponent> ent, ref ComponentInit args)
    {
        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, ent.Comp.Sealed);
    }

    private void OnSealableUnequipped(Entity<ModSuitSealableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        // cant be sealed when not worn
        SetSeal((ent.Owner, ent.Comp), false);
    }

    [PublicAPI]
    public bool SetSeal(Entity<ModSuitSealableComponent?> ent, bool value)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // prevent sending events and excessive dirtying
        if (ent.Comp.Sealed == value)
            return true;

        ent.Comp.Sealed = value;
        Dirty(ent);

        if (value)
        {
            var ev = new ModSuitSealedEvent();
            RaiseLocalEvent(ent.Owner, ev);

        }
        else
        {
            var ev = new ModSuitUnsealedEvent();
            RaiseLocalEvent(ent.Owner, ev);
        }

        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, value);
        _item.VisualsChanged(ent.Owner);

        return true;
    }

    #endregion
}
