using System.Diagnostics.CodeAnalysis;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;
using JetBrains.Annotations;

namespace Content.Shared.Modules.Modules;

public sealed partial class ModuleContainedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleContainedComponent, ModuleAddedContainerEvent>(OnContainedAdded);
        SubscribeLocalEvent<ModuleContainedComponent, ModuleRemovedContainerEvent>(OnContainedRemoved);
        SubscribeLocalEvent<ModuleContainedComponent, ModuleRelayedEvent<ModSuitGetUiEntriesEvent>>(OnGetModSuitUiState);
    }

    private void OnContainedAdded(Entity<ModuleContainedComponent> ent, ref ModuleAddedContainerEvent args)
    {
        ent.Comp.Container = args.Container;
        Dirty(ent);
    }

    private void OnContainedRemoved(Entity<ModuleContainedComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        ent.Comp.Container = null;
        Dirty(ent);
    }

    private ModSuitBaseModuleBuiEntry GetDefaultModSuitBui(Entity<ModuleContainedComponent> ent)
    {
        return new ModSuitBaseModuleBuiEntry(CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity);
    }

    private void OnGetModSuitUiState(Entity<ModuleContainedComponent> ent, ref ModuleRelayedEvent<ModSuitGetUiEntriesEvent> args)
    {
        ModSuitModuleBuiEntry? foundEntry = null;

        foreach (var entry in args.Args.Entries)
        {
            if (entry is not ModSuitModuleBuiEntry moduleEntry)
                continue;

            foundEntry = moduleEntry;
            break;
        }

        if (foundEntry == null)
            return; // should never happen as the modsuit adds a blank one to reset any lingering modules

        var ev = new ModSuitGetModuleUiEvent();
        RaiseLocalEvent(ent.Owner, ev);

        var netEntity = GetNetEntity(ent.Owner);
        var buiEntry = GetDefaultModSuitBui(ent);

        foreach (var entry in ev.BuiEntries)
        {
            if (entry.Priority < buiEntry.Priority)
                continue;

            buiEntry = entry;
        }

        var newModules = new KeyValuePair<NetEntity, ModSuitBaseModuleBuiEntry>[foundEntry.Modules.Length + 1];
        Array.Copy(foundEntry.Modules, newModules, foundEntry.Modules.Length);
        newModules[^1] = KeyValuePair.Create(netEntity, buiEntry);
        foundEntry.Modules = newModules;
    }

    [PublicAPI]
    public bool TryGetContainer(Entity<ModuleContainedComponent?> ent, [NotNullWhen(true)] out EntityUid? container)
    {
        container = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        container = ent.Comp.Container;
        return container != null;
    }

    [PublicAPI]
    public EntityUid? GetContainer(Entity<ModuleContainedComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return null;

        return ent.Comp.Container;
    }

    [PublicAPI]
    public bool TryGetUser(Entity<ModuleContainedComponent?> ent, [NotNullWhen(true)] out EntityUid? user)
    {
        user = null;

        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (ent.Comp.Container == null)
            return false;

        var ev = new ModuleGetUserEvent();
        RaiseLocalEvent(ent.Comp.Container.Value, ev);

        user = ev.User;
        return ev.User != null;
    }
}
