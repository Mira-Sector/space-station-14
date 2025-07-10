using Content.Shared.Buckle.Components;
using Content.Shared.Surgery.Components;
using Content.Shared.Surgery.Events;
using Content.Shared.Surgery.UI;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private void InitializeUI()
    {
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSinkComponent, GetSurgeryUiTarget>(OnLinkedSinkGetTarget);
        SubscribeLocalEvent<SurgeryUserInterfaceLinkedSourceComponent, GetSurgeryUiTarget>(OnLinkedSourceGetTarget);

        Subs.BuiEvents<SurgeryUserInterfaceComponent>(SurgeryUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
        });
    }

    private void OnUiOpened(Entity<SurgeryUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!TryGetTarget(ent.Owner, out var target))
            return;

        var state = new SurgeryBoundUserInterfaceState(GetNetEntity(target.Value));
        Ui.SetUiState(ent.Owner, args.UiKey, state);
    }

    private void OnLinkedSinkGetTarget(Entity<SurgeryUserInterfaceLinkedSinkComponent> ent, ref GetSurgeryUiTarget args)
    {
        /*
        if (ent.Comp.Source is not { } source)
            return;
        */

        var sources = _lookup.GetEntitiesInRange<SurgeryUserInterfaceLinkedSourceComponent>(Transform(ent.Owner).Coordinates, 20f);
        foreach (var source in sources)
        {
            if (!TryGetTarget(source, out var target))
                continue;


            args.Target = target;
            return;
        }
    }

    private void OnLinkedSourceGetTarget(Entity<SurgeryUserInterfaceLinkedSourceComponent> ent, ref GetSurgeryUiTarget args)
    {
        if (!TryComp<StrapComponent>(ent.Owner, out var strap))
            return;

        if (strap.BuckledEntities.Count != 1)
            return;

        args.Target = strap.BuckledEntities.First();
    }

    private bool TryGetTarget(EntityUid uid, [NotNullWhen(true)] out EntityUid? target)
    {
        var ev = new GetSurgeryUiTarget();
        RaiseLocalEvent(uid, ref ev);

        target = ev.Target;
        return target != null;
    }
}
