using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public abstract partial class BaseToggleableModuleSystem<T> : BaseModuleSystem<T> where T : BaseToggleableModuleComponent
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    // not stored on the component as its an abstract component
    internal Dictionary<NetEntity, TimeSpan> NextMessage = [];
    internal static readonly TimeSpan MessageDelay = TimeSpan.FromSeconds(0.25f);

    [MustCallBase]
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<T, ModuleDisabledEvent>(OnDisabled);
    }

    [MustCallBase]
    protected virtual void OnEnabled(Entity<T> ent, ref ModuleEnabledEvent args)
    {
        ent.Comp.Toggled = true;
    }

    [MustCallBase]
    protected virtual void OnDisabled(Entity<T> ent, ref ModuleDisabledEvent args)
    {
        ent.Comp.Toggled = false;
    }

    [PublicAPI]
    public void Toggle(Entity<T?> ent, EntityUid? user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        Toggle(ent, !ent.Comp.Toggled, user);
    }

    [PublicAPI]
    public void Toggle(Entity<T?> ent, bool toggle, EntityUid? user)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Toggled == toggle)
            return;

        if (ent.Comp.Container == null)
            return;

        var beforeEv = new ModuleToggleAttemptEvent(toggle, ent.Comp.Container.Value, user);
        RaiseLocalEvent(ent.Owner, beforeEv);

        if (beforeEv.Cancelled)
        {
            if (user == null || beforeEv.Reason == null)
                return;

            var netUser = GetNetEntity(user.Value);

            if (NextMessage.TryGetValue(netUser, out var nextMessage))
            {
                if (nextMessage > Timing.CurTime)
                    return;
            }

            nextMessage = Timing.CurTime + MessageDelay;
            NextMessage[netUser] = nextMessage;

            _popup.PopupPredicted(Loc.GetString(beforeEv.Reason), ent.Owner, user);
            return;
        }

        RaiseToggleEvents((ent.Owner, ent.Comp), toggle, user);
    }

    [MustCallBase]
    protected virtual void RaiseToggleEvents(Entity<T> ent, bool toggle, EntityUid? user)
    {
        if (toggle)
        {
            var ev = new ModuleEnabledEvent(ent.Comp.Container!.Value, user);
            RaiseLocalEvent(ent.Owner, ev);
        }
        else
        {

            var ev = new ModuleDisabledEvent(ent.Comp.Container!.Value, user);
            RaiseLocalEvent(ent.Owner, ev);
        }
    }
}
