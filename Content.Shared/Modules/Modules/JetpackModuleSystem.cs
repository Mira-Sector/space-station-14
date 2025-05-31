using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;

namespace Content.Shared.Modules.Modules;

public sealed partial class JetpackModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;
    [Dependency] private readonly ModuleContainedSystem _moduleContained = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JetpackModuleComponent, ModuleEnabledEvent>(OnEnabled);
        SubscribeLocalEvent<JetpackModuleComponent, ModuleDisabledEvent>(OnDisabled);

        SubscribeLocalEvent<JetpackModuleComponent, ModuleToggleAttemptEvent>(OnToggleAttempt);
        SubscribeLocalEvent<JetpackModuleComponent, EnableJetpackAttemptEvent>(OnJetpackAttempt);
    }

    private void OnEnabled(Entity<JetpackModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        if (!CanEnable(ent))
            return;

        if (!_jetpack.CanEnableOnGrid(Transform(args.Container).GridUid))
            return;

        EnsureComp<JetpackComponent>(args.Container, out var jetpack);
        _jetpack.SetEnabled(args.Container, jetpack, true, args.User);
    }

    private void OnDisabled(Entity<JetpackModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        _jetpack.SetEnabled(args.Container, Comp<JetpackComponent>(args.Container), false, args.User);
    }

    private void OnToggleAttempt(Entity<JetpackModuleComponent> ent, ref ModuleToggleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (CanEnable(ent))
            return;

        // jetpack component shows its own warning
        args.Reason = null;
        args.Cancel();
    }

    private void OnJetpackAttempt(Entity<JetpackModuleComponent> ent, ref EnableJetpackAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!CanEnable(ent))
            args.Cancel();
    }

    internal bool CanEnable(Entity<JetpackModuleComponent> ent)
    {
        // we never intended to draw power so abort
        if (!HasComp<PowerDrainModuleComponent>(ent.Owner))
            return true;

        if (!_moduleContained.TryGetContainer(ent.Owner, out var container))
            return false;

        if (!TryComp<PowerCellDrawComponent>(container.Value, out var drawComp))
            return false;

        return drawComp.CanDraw;
    }
}
