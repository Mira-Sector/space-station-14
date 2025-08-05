using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableModuleComponent, ModuleRemovedContainerEvent>(OnRemoved);
        SubscribeLocalEvent<ToggleableModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<ToggleableModuleComponent, ModuleDisabledEvent>(OnDisabled);
    }

    private void OnRemoved(Entity<ToggleableModuleComponent> ent, ref ModuleRemovedContainerEvent args)
    {
        ent.Comp.Toggled = false;
        Dirty(ent);
        RaiseToggleEvents(ent, null);
    }

    private void OnEnabled(Entity<ToggleableModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        ent.Comp.Toggled = true;
        Dirty(ent);
    }

    private void OnDisabled(Entity<ToggleableModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        ent.Comp.Toggled = false;
        Dirty(ent);
    }

    [PublicAPI]
    public bool IsToggled(Entity<ToggleableModuleComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return ent.Comp.Toggled;
    }

    [PublicAPI]
    public void Toggle(Entity<ToggleableModuleComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        Toggle(ent, !ent.Comp.Toggled, user);
    }

    [PublicAPI]
    public void Toggle(Entity<ToggleableModuleComponent?> ent, bool toggle, EntityUid? user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Toggled == toggle)
            return;

        if (!_moduleContained.TryGetContainer(ent.Owner, out var container))
            return;

        var beforeEv = new ModuleToggleAttemptEvent(toggle, container.Value, user);
        RaiseLocalEvent(ent.Owner, beforeEv);

        if (beforeEv.Cancelled)
        {
            if (user == null || beforeEv.Reason == null)
                return;

            if (ent.Comp.NextMessage > _timing.CurTime)
                return;

            _popup.PopupPredicted(Loc.GetString(beforeEv.Reason), ent.Owner, user);
            ent.Comp.NextMessage = _timing.CurTime + ent.Comp.MessageDelay;
            return;
        }

        ent.Comp.Toggled = toggle;
        Dirty(ent);
        RaiseToggleEvents((ent.Owner, ent.Comp), user);
    }

    private void RaiseToggleEvents(Entity<ToggleableModuleComponent> ent, EntityUid? user)
    {
        RaiseToggleEvents(ent.AsNullable(), ent.Comp.Toggled, user);
    }

    [PublicAPI]
    public void RaiseToggleEvents(Entity<ToggleableModuleComponent?> ent, bool toggled, EntityUid? user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!_moduleContained.TryGetContainer(ent.Owner, out var container))
            return;

        if (toggled)
        {
            var ev = new ModuleEnabledEvent(container.Value, user);
            RaiseLocalEvent(ent.Owner, ev);
        }
        else
        {

            var ev = new ModuleDisabledEvent(container.Value, user);
            RaiseLocalEvent(ent.Owner, ev);
        }
    }
}
