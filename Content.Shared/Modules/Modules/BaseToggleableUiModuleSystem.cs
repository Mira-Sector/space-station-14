using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;

namespace Content.Shared.Modules.Modules;

public abstract partial class BaseToggleableUiModuleSystem<T> : BaseToggleableModuleSystem<T> where T : BaseToggleableUiModuleComponent
{
    [Dependency] protected readonly SharedModSuitSystem ModSuit = default!;

    // not stored on the component as its an abstract component
    internal readonly Dictionary<NetEntity, TimeSpan> NextUpdate = [];
    internal static readonly TimeSpan UpdateDelay = TimeSpan.FromSeconds(0.25f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ModSuitToggleButtonMessage>(OnToggleButton);
    }

    protected override ModSuitBaseModuleBuiEntry GetModSuitModuleBuiEntry(Entity<T> ent)
    {
        return new ModSuitBaseToggleableModuleBuiEntry(ent.Comp.Toggled, CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity);
    }

    protected override void RaiseToggleEvents(Entity<T> ent, bool toggle, EntityUid? user)
    {
        base.RaiseToggleEvents(ent, toggle, user);

        if (CanUpdate(ent))
            ModSuit.UpdateUI(ent.Comp.Container!.Value);
    }

    private void OnToggleButton(ModSuitToggleButtonMessage args)
    {
        var module = GetEntity(args.Module);

        if (!TryComp<T>(module, out var component))
            return;

        if (component.Container is not { } container)
            return;

        if (!CanUpdate((module, component)))
            return;

        Toggle((module, component), args.Toggle, GetEntity(args.User));
        ModSuit.UpdateUI(container);
    }

    internal bool CanUpdate(Entity<T> ent)
    {
        var netContainer = GetNetEntity(ent.Comp.Container!.Value);
        if (NextUpdate.TryGetValue(netContainer, out var nextUpdate))
        {
            if (nextUpdate > Timing.CurTime)
                return false;
        }

        nextUpdate = Timing.CurTime + UpdateDelay;
        NextUpdate[netContainer] = nextUpdate;
        return true;
    }
}
