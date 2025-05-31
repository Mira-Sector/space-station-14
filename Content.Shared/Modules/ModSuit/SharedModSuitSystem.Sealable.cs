using Content.Shared.Clothing;
using Content.Shared.Item;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    private void InitializeSealable()
    {
        SubscribeLocalEvent<ModSuitSealableComponent, ComponentInit>(OnSealableInit);
        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotUnequippedEvent>(OnSealableUnequipped);

        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitGetUiStatesEvent>(OnSealableGetUiStates);
        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitDeployableRelayedEvent<ModSuitGetUiStatesEvent>>((u, c, a) => OnSealableGetUiStates((u, c), ref a.Args));

        SubscribeAllEvent<ModSuitSealButtonMessage>(OnSealableUiButton);
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

    private void OnSealableGetUiStates(Entity<ModSuitSealableComponent> ent, ref ModSuitGetUiStatesEvent args)
    {
        var type = CompOrNull<ModSuitPartTypeComponent>(ent.Owner)?.Type ?? ModSuitPartType.Other;
        var newData = new ModSuitSealableBuiEntry(ent.Comp.UiLayer, type, ent.Comp.Sealed);
        var toAdd = KeyValuePair.Create(GetNetEntity(ent.Owner), newData);

        ModSuitSealableBoundUserInterfaceState? foundState = null;

        // find any state that another part may have added
        foreach (var state in args.States)
        {
            if (state is not ModSuitSealableBoundUserInterfaceState sealableState)
                continue;

            foundState = sealableState;
            break;
        }

        var length = (foundState?.Parts.Length ?? 0) + 1;
        var parts = new KeyValuePair<NetEntity, ModSuitSealableBuiEntry>[length];

        if (foundState == null)
        {
            parts = [toAdd];
            var newState = new ModSuitSealableBoundUserInterfaceState(parts);
            args.States.Add(newState);
            return;
        }

        var inserted = false;
        var newIndex = 0;

        for (var i = 0; i < foundState.Parts.Length; i++)
        {
            var existing = foundState.Parts[i];
            var (_, data) = existing;

            if (!inserted && data.Type > type)
            {
                parts[newIndex++] = toAdd;
                inserted = true;
            }

            parts[newIndex++] = existing;
        }

        if (!inserted)
            parts[newIndex] = toAdd;

        foundState.Parts = parts;
    }

    private void OnSealableUiButton(ModSuitSealButtonMessage args)
    {
        SetSeal(GetEntity(args.Part), args.ShouldSeal);
    }

    [PublicAPI]
    public bool IsSealed(Entity<ModSuitSealableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return ent.Comp.Sealed;
    }

    [PublicAPI]
    public bool SetSeal(Entity<ModSuitSealableComponent?> ent, bool isSealed)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // prevent sending events and excessive dirtying
        if (ent.Comp.Sealed == isSealed)
            return true;

        ent.Comp.Sealed = isSealed;
        Dirty(ent);

        var container = CompOrNull<ModSuitDeployedPartComponent>(ent.Owner)?.Suit ?? ent.Owner;
        RaiseSealableEvent(ent.Owner, container, isSealed);

        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, isSealed);
        _item.VisualsChanged(ent.Owner);

        UpdateUI(container);

        return true;
    }

    internal void RaiseSealableEvent(EntityUid part, EntityUid suit, bool isSealed)
    {
        if (isSealed)
        {
            var partEv = new ModSuitSealedEvent();
            RaiseLocalEvent(part, partEv);

            var suitEv = new ModSuitContainerPartSealedEvent(part);
            RaiseLocalEvent(suit, suitEv);
        }
        else
        {
            var partEv = new ModSuitUnsealedEvent();
            RaiseLocalEvent(part, partEv);

            var suitEv = new ModSuitContainerPartUnsealedEvent(part);
            RaiseLocalEvent(suit, suitEv);
        }

    }
}
