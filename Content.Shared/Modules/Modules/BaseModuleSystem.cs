using System.Diagnostics.CodeAnalysis;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
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

    protected virtual ModSuitModuleBaseModuleBuiEntry GetModSuitModuleBuiEntry(Entity<T> ent)
    {
        return new ModSuitModuleBaseModuleBuiEntry();
    }

    private void OnGetModSuitUiState(Entity<T> ent, ref ModuleRelayedEvent<ModSuitGetUiStatesEvent> args)
    {
        var netEntity = GetNetEntity(ent.Owner);
        var newData = GetModSuitModuleBuiEntry(ent);
        var toAdd = KeyValuePair.Create(netEntity, newData);

        ModSuitModuleBoundtUserInterfaceState? foundState = null;

        foreach (var state in args.Args.States)
        {
            if (state is not ModSuitModuleBoundtUserInterfaceState moduleState)
                continue;

            foundState = moduleState;
            break;
        }

        var index = foundState?.Modules.Length ?? 0;
        var length = index + 1;
        var modules = new KeyValuePair<NetEntity, ModSuitModuleBaseModuleBuiEntry>[length];

        if (foundState == null)
        {
            modules = [toAdd];
            var newState = new ModSuitModuleBoundtUserInterfaceState(modules);
            args.Args.States.Add(newState);
            return;
        }

        //check we havent been added already
        foreach (var (module, _) in foundState.Modules)
        {
            if (module == netEntity)
                return;
        }

        modules[index] = toAdd;
        foundState.Modules = modules;
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
