using Content.Shared.Modules.Components.Modules;
using Content.Shared.Modules.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.PowerCell;

namespace Content.Shared.Modules.Modules;

public sealed partial class JetpackModuleSystem : BaseToggleableModuleSystem<JetpackModuleComponent>
{
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JetpackModuleComponent, ModuleToggleAttemptEvent>(OnToggleAttempt);
        SubscribeLocalEvent<JetpackModuleComponent, EnableJetpackAttemptEvent>(OnJetpackAttempt);
    }

    protected override void OnEnabled(Entity<JetpackModuleComponent> ent, ref ModuleEnabledEvent args)
    {
        base.OnEnabled(ent, ref args);

        if (!CanEnable(ent))
            return;

        if (!_jetpack.CanEnableOnGrid(Transform(args.Container).GridUid))
            return;

        _jetpack.SetEnabled(ent.Owner, Comp<JetpackComponent>(ent.Owner), true, args.User);
    }

    protected override void OnDisabled(Entity<JetpackModuleComponent> ent, ref ModuleDisabledEvent args)
    {
        base.OnDisabled(ent, ref args);

        _jetpack.SetEnabled(ent.Owner, Comp<JetpackComponent>(ent.Owner), false, args.User);
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

        if (ent.Comp.Container == null)
            return false;

        if (!TryComp<PowerCellDrawComponent>(ent.Comp.Container.Value, out var drawComp))
            return false;

        return drawComp.CanDraw;
    }
}
