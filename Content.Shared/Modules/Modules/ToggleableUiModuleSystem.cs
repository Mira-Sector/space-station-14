using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Modules.ModSuit;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using Content.Shared.Modules.ModSuit.UI.Modules;
using Robust.Shared.Timing;

namespace Content.Shared.Modules.Modules;

public sealed partial class ToggleableUiModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedModSuitSystem _modSuit = default!;
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;
    [Dependency] private readonly ToggleableModuleSystem _toggleableModule = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // not stored on the component as its an abstract component
    internal readonly Dictionary<NetEntity, TimeSpan> NextUpdate = [];
    internal static readonly TimeSpan UpdateDelay = TimeSpan.FromSeconds(0.25f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableUiModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<ToggleableUiModuleComponent, ModuleDisabledEvent>(OnDisabled);

        SubscribeLocalEvent<ToggleableUiModuleComponent, ModSuitGetModuleUiEvent>(OnGetModSuitUi);

        SubscribeAllEvent<ModSuitToggleButtonMessage>(OnToggleButton);
    }

    private void OnToggleButton(ModSuitToggleButtonMessage args)
    {
        var module = GetEntity(args.Module);

        if (!_moduleContained.TryGetContainer(module, out var container))
            return;

        if (!CanUpdate(container.Value))
            return;

        _toggleableModule.Toggle(module, args.Toggle, GetEntity(args.User));
        _modSuit.UpdateUI(container.Value);
    }

    private void OnEnabled(Entity<ToggleableUiModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        if (_moduleContained.TryGetContainer(ent.Owner, out var container) && CanUpdate(container.Value))
            _modSuit.UpdateUI(container.Value);
    }

    private void OnDisabled(Entity<ToggleableUiModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        if (_moduleContained.TryGetContainer(ent.Owner, out var container) && CanUpdate(container.Value))
            _modSuit.UpdateUI(container.Value);
    }

    private void OnGetModSuitUi(Entity<ToggleableUiModuleComponent> ent, ref ModSuitGetModuleUiEvent args)
    {
        args.BuiEntries.Add(new ModSuitBaseToggleableModuleBuiEntry(
                _toggleableModule.IsToggled(ent.Owner),
                CompOrNull<ModSuitModuleComplexityComponent>(ent.Owner)?.Complexity
            )
        );
    }

    internal bool CanUpdate(EntityUid container)
    {
        var netContainer = GetNetEntity(container);
        if (NextUpdate.TryGetValue(netContainer, out var nextUpdate))
        {
            if (nextUpdate > _timing.CurTime)
                return false;
        }

        nextUpdate = _timing.CurTime + UpdateDelay;
        NextUpdate[netContainer] = nextUpdate;
        return true;
    }
}
