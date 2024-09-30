using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Fluids.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.Fluids;

/// <summary>
/// Mopping logic for interacting with puddle components.
/// </summary>
public abstract class SharedAbsorbentSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, ComponentGetState>(OnAbsorbentGetState);
        SubscribeLocalEvent<AbsorbentComponent, ComponentHandleState>(OnAbsorbentHandleState);

        SubscribeLocalEvent<AbsorbentToggleComponent, MapInitEvent>(OnAbsorbentToggleInit);
        SubscribeLocalEvent<AbsorbentToggleComponent, AbsorbentActionEvent>(OnAbsorbentToggleAction);

        SubscribeLocalEvent<AbsorbentToggleComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<AbsorbentToggleComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnAbsorbentHandleState(EntityUid uid, AbsorbentComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AbsorbentComponentState state)
            return;

        if (component.Progress.OrderBy(x => x.Key.ToArgb()).SequenceEqual(state.Progress))
            return;

        component.Progress.Clear();
        foreach (var item in state.Progress)
        {
            component.Progress.Add(item.Key, item.Value);
        }
    }

    private void OnAbsorbentGetState(EntityUid uid, AbsorbentComponent component, ref ComponentGetState args)
    {
        args.State = new AbsorbentComponentState(component.Progress);
    }

    private void OnAbsorbentToggleInit(EntityUid uid, AbsorbentToggleComponent component, ref MapInitEvent args)
    {
        if (component.AbsorbentAction != null)
            return;

        _actions.AddAction(uid, ref component.AbsorbentAction, component.ToggleActionId, uid);

        Dirty(uid, component);
    }

    private void OnAbsorbentToggleAction(EntityUid uid, AbsorbentToggleComponent component, InstantActionEvent args)
    {
        if (args.Handled == true)
            return;

        component.Enabled ^= true;
        Dirty(uid, component);

        args.Handled = true;
    }

    private void OnStrapped(EntityUid uid, AbsorbentToggleComponent component, ref StrappedEvent args)
    {
        _actions.AddAction(args.Buckle, ref component.AbsorbentAction, component.ToggleActionId, uid);
        Dirty(uid, component);
    }

    private void OnUnstrapped(EntityUid uid, AbsorbentToggleComponent component, ref UnstrappedEvent args)
    {
        _actions.RemoveAction(args.Buckle, component.AbsorbentAction);
        Dirty(uid, component);
    }

    [Serializable, NetSerializable]
    protected sealed class AbsorbentComponentState : ComponentState
    {
        public Dictionary<Color, float> Progress;

        public AbsorbentComponentState(Dictionary<Color, float> progress)
        {
            Progress = progress;
        }
    }
}
