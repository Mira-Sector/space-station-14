using Content.Shared.Clothing;
using Content.Shared.Item;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    private void InitializeSealable()
    {
        SubscribeLocalEvent<ModSuitSealableComponent, ComponentInit>(OnSealableInit);
        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotUnequippedEvent>(OnSealableUnequipped);
        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitDeployableRelayedEvent<ModSuitGetUiStatesEvent>>(OnSealableGetUiStates);
    }

    private void OnSealableInit(Entity<ModSuitSealableComponent> ent, ref ComponentInit args)
    {
        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, ent.Comp.Sealed);
    }

    private void OnSealableUnequipped(Entity<ModSuitSealableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        // cant be sealed when not worn
        SetSeal((ent.Owner, ent.Comp), false);
    }

    private void OnSealableGetUiStates(Entity<ModSuitSealableComponent> ent, ref ModSuitDeployableRelayedEvent<ModSuitGetUiStatesEvent> args)
    {
        var state = new ModSuitSealableBoundUserInterfaceState(GetNetEntity(ent.Owner), ent.Comp.Sealed);
        args.Args.States.Add(state);
    }

    [PublicAPI]
    public bool SetSeal(Entity<ModSuitSealableComponent?> ent, bool value)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // prevent sending events and excessive dirtying
        if (ent.Comp.Sealed == value)
            return true;

        ent.Comp.Sealed = value;
        Dirty(ent);

        if (value)
        {
            var ev = new ModSuitSealedEvent();
            RaiseLocalEvent(ent.Owner, ev);

        }
        else
        {
            var ev = new ModSuitUnsealedEvent();
            RaiseLocalEvent(ent.Owner, ev);
        }

        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, value);
        _item.VisualsChanged(ent.Owner);

        if (TryComp<ModSuitDeployedPartComponent>(ent.Owner, out var deployedPart))
            UpdateUI(deployedPart.Suit);

        return true;
    }
}
