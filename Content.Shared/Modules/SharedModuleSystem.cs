using Content.Shared.Hands.EntitySystems;
using Content.Shared.Modules.Components;
using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.Modules;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Modules;

public abstract partial class SharedModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializePower();
        InitializeRelay();
        InitializeRequirements();
        InitializeSlot();

        SubscribeLocalEvent<ModuleContainerComponent, ComponentInit>(OnContainerInit);
        SubscribeLocalEvent<ModuleContainerComponent, ContainerIsInsertingAttemptEvent>(OnContainerAttempt);
        SubscribeLocalEvent<ModuleContainerComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<ModuleContainerComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);

        SubscribeLocalEvent<ModuleContainerComponent, ModSuitGetUiEntriesEvent>(OnGetModSuitUiEntry);

        SubscribeAllEvent<ModSuitEjectButtonMessage>(OnModSuitEjectButton);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnContainerInit(Entity<ModuleContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Modules = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModuleContainerId);
    }

    private void OnContainerAttempt(Entity<ModuleContainerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Container.ID != ent.Comp.ModuleContainerId)
            return;

        if (!_whitelist.CheckBoth(args.EntityUid, ent.Comp.Blacklist, ent.Comp.Whitelist))
        {
            args.Cancel();
            return;
        }

        var containerEv = new ModuleContainerModuleAddingAttemptEvent(args.EntityUid);
        RaiseLocalEvent(ent.Owner, containerEv);

        if (containerEv.Cancelled)
        {
            if (_net.IsServer && containerEv.Reason != null)
                _popup.PopupEntity(Loc.GetString(containerEv.Reason), ent.Owner);

            args.Cancel();
            return;
        }

        var moduleEv = new ModuleAddingAttemptContainerEvent(ent.Owner);
        RaiseLocalEvent(args.EntityUid, moduleEv);

        if (moduleEv.Cancelled)
        {
            if (_net.IsServer && moduleEv.Reason != null)
                _popup.PopupEntity(Loc.GetString(moduleEv.Reason), ent.Owner);

            args.Cancel();
            return;
        }
    }

    private void OnContainerInserted(Entity<ModuleContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ModuleContainerId)
            return;

        var containerEv = new ModuleContainerModuleAddedEvent(args.Entity);
        RaiseLocalEvent(ent.Owner, containerEv);

        var moduleEv = new ModuleAddedContainerEvent(ent.Owner);
        RaiseLocalEvent(args.Entity, moduleEv);
    }

    private void OnContainerRemoved(Entity<ModuleContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.ModuleContainerId)
            return;

        var containerEv = new ModuleContainerModuleRemovedEvent(args.Entity);
        RaiseLocalEvent(ent.Owner, containerEv);

        var moduleEv = new ModuleRemovedContainerEvent(ent.Owner);
        RaiseLocalEvent(args.Entity, moduleEv);
    }

    private void OnGetModSuitUiEntry(Entity<ModuleContainerComponent> ent, ref ModSuitGetUiEntriesEvent args)
    {
        // add a blank one so we override incase there is no more modules
        foreach (var entry in args.Entries)
        {
            if (entry is ModSuitModuleBuiEntry)
                return;
        }

        args.Entries.Add(new ModSuitModuleBuiEntry());
        RelayToModules(ent, ref args);
    }

    private void OnModSuitEjectButton(ModSuitEjectButtonMessage args)
    {
        var module = GetEntity(args.Module);
        var container = GetEntity(args.Container);

        if (_moduleContained.GetContainer(module) != container)
            return;

        if (!TryComp<ModuleContainerComponent>(container, out var moduleContainer))
            return;

        _container.Remove(module, moduleContainer.Modules);
        _hands.TryPickup(GetEntity(args.User), module);
    }

    [PublicAPI]
    public IEnumerable<EntityUid> GetModules(Entity<ModuleContainerComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        foreach (var module in ent.Comp.Modules.ContainedEntities)
            yield return module;
    }

    [PublicAPI]
    public void RaiseEventToModules<T>(Entity<ModuleContainerComponent?> ent, T ev) where T : notnull
    {
        foreach (var module in GetModules(ent))
            RaiseLocalEvent(module, ev);
    }

    [PublicAPI]
    public void RaiseEventToContainer<T>(Entity<ModuleContainedComponent?> ent, T ev) where T : notnull
    {
        if (_moduleContained.TryGetContainer(ent, out var container))
            RaiseLocalEvent(container.Value, ev);
    }

    [PublicAPI]
    public void UpdateUis(EntityUid uid)
    {
        _modSuit.UpdateUI(uid);
    }
}
