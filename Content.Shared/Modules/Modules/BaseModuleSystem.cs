using System.Diagnostics.CodeAnalysis;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;
using JetBrains.Annotations;

namespace Content.Shared.Modules.Modules;

public abstract partial class BaseModuleSystem<T> : EntitySystem where T : BaseModuleComponent
{
    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ModuleAddedContainerEvent>(OnAdded);
        SubscribeLocalEvent<T, ModuleRemovedContainerEvent>(OnRemoved);
        SubscribeLocalEvent<T, ModuleRelayedEvent<ModSuitGetUiStatesEvent>>(OnGetModSuitUiState);
    }

    [MustCallBase]
    protected virtual void OnAdded(Entity<T> ent, ref ModuleAddedContainerEvent args)
    {
        ent.Comp.Container = args.Container;
    }

    [MustCallBase]
    protected virtual void OnRemoved(Entity<T> ent, ref ModuleRemovedContainerEvent args)
    {
        ent.Comp.Container = null;
    }

    protected virtual ModSuitBaseModuleBuiEntry GetModSuitModuleBuiEntry(Entity<T> ent)
    {
        return new ModSuitBaseModuleBuiEntry(CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity);
    }

    private void OnGetModSuitUiState(Entity<T> ent, ref ModuleRelayedEvent<ModSuitGetUiStatesEvent> args)
    {
        var netEntity = GetNetEntity(ent.Owner);
        var newData = GetModSuitModuleBuiEntry(ent);
        var toAdd = KeyValuePair.Create(netEntity, newData);

        ModSuitModuleBoundUserInterfaceState? foundState = null;

        foreach (var state in args.Args.States)
        {
            if (state is not ModSuitModuleBoundUserInterfaceState moduleState)
                continue;

            foundState = moduleState;
            break;
        }

        if (foundState == null)
            return; // should never happen

        for (var i = 0; i < foundState.Modules.Length; i++)
        {
            var existing = foundState.Modules[i];
            var (existingModule, existingData) = existing;

            if (existingModule != netEntity)
                continue;

            // they override us
            if (existingData.Priority > newData.Priority)
                return;

            foundState.Modules[i] = toAdd;
            return;
        }

        var newModules = new KeyValuePair<NetEntity, ModSuitBaseModuleBuiEntry>[foundState.Modules.Length + 1];
        Array.Copy(foundState.Modules, newModules, foundState.Modules.Length);
        newModules[^1] = toAdd;
        foundState.Modules = newModules;
    }

    [PublicAPI]
    public bool TryGetUser(Entity<T> ent, [NotNullWhen(true)] out EntityUid? user)
    {
        user = null;

        if (ent.Comp.Container == null)
            return false;

        var ev = new ModuleGetUserEvent();
        RaiseLocalEvent(ent.Comp.Container.Value, ev);

        user = ev.User;
        return ev.User != null;
    }
}
