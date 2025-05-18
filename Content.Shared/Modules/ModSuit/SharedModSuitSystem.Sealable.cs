using Content.Shared.Clothing;
using Content.Shared.Item;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using JetBrains.Annotations;
using System.Linq;
using YamlDotNet.Core.Tokens;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly SharedItemSystem _item = default!;

    private void InitializeSealable()
    {
        SubscribeLocalEvent<ModSuitSealableComponent, ComponentInit>(OnSealableInit);
        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotUnequippedEvent>(OnSealableUnequipped);
        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitDeployableRelayedEvent<ModSuitGetUiStatesEvent>>(OnSealableGetUiStates);

        SubscribeLocalEvent<ModSuitUserInterfaceComponent, ModSuitSealButtonMessage>(OnSealableUiButton);
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
        var type = CompOrNull<ModSuitPartTypeComponent>(ent.Owner)?.Type ?? ModSuitPartType.Other;
        var toAdd = new ModSuitSealableBuiEntry(ent.Comp.UiLayer, type, ent.Comp.Sealed);

        ModSuitSealableBoundUserInterfaceState? foundState = null;

        // find any state that another part may have added
        foreach (var state in args.Args.States)
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
            parts = [KeyValuePair.Create(GetNetEntity(ent.Owner), toAdd)];
            var newState = new ModSuitSealableBoundUserInterfaceState(parts);
            args.Args.States.Add(newState);
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
                parts[newIndex++] = KeyValuePair.Create(GetNetEntity(ent.Owner), toAdd);
                inserted = true;
            }

            parts[newIndex++] = existing;
        }

        if (!inserted)
            parts[newIndex] = KeyValuePair.Create(GetNetEntity(ent.Owner), toAdd);

        foundState.Parts = parts;
    }

    private void OnSealableUiButton(Entity<ModSuitUserInterfaceComponent> ent, ref ModSuitSealButtonMessage args)
    {
        SetSeal(GetEntity(args.Part), args.ShouldSeal);
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
