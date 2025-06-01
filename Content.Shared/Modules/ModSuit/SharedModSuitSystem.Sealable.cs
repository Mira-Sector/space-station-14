using Content.Shared.Clothing;
using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using Content.Shared.Modules.ModSuit.UI;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private void InitializeSealable()
    {
        SubscribeLocalEvent<ModSuitSealableComponent, ComponentInit>(OnSealableInit);
        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotUnequippedEvent>(OnSealableUnequipped);

        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitGetUiEntriesEvent>(OnSealableGetUiEntries);
        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitDeployableRelayedEvent<ModSuitGetUiEntriesEvent>>((u, c, a) => OnSealableGetUiEntries((u, c), ref a.Args));

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

    private void OnSealableGetUiEntries(Entity<ModSuitSealableComponent> ent, ref ModSuitGetUiEntriesEvent args)
    {
        var type = CompOrNull<ModSuitPartTypeComponent>(ent.Owner)?.Type ?? ModSuitPartType.Other;
        var newData = new ModSuitSealablePartBuiEntry(ent.Comp.UiLayer, type, ent.Comp.Sealed);
        var toAdd = KeyValuePair.Create(GetNetEntity(ent.Owner), newData);

        ModSuitSealableBuiEntry? foundEntry = null;

        // find any state that another part may have added
        foreach (var entry in args.Entries)
        {
            if (entry is not ModSuitSealableBuiEntry sealableEntry)
                continue;

            foundEntry = sealableEntry;
            break;
        }

        var length = (foundEntry?.Parts.Length ?? 0) + 1;
        var parts = new KeyValuePair<NetEntity, ModSuitSealablePartBuiEntry>[length];

        if (foundEntry == null)
        {
            parts = [toAdd];
            var newEntry = new ModSuitSealableBuiEntry(parts);
            args.Entries.Add(newEntry);
            return;
        }

        var insertIndex = foundEntry.Parts.Length;
        for (var i = 0; i < foundEntry.Parts.Length; i++)
        {
            if (foundEntry.Parts[i].Value.Type > type)
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex > 0)
            Array.Copy(foundEntry.Parts, parts, insertIndex);

        parts[insertIndex] = toAdd;

        if (insertIndex < foundEntry.Parts.Length)
            Array.Copy(foundEntry.Parts, insertIndex, parts, insertIndex + 1, foundEntry.Parts.Length - insertIndex);

        foundEntry.Parts = parts;
    }

    private void OnSealableUiButton(ModSuitSealButtonMessage args)
    {
        foreach (var (part, shouldSeal) in args.Parts)
            SetSeal(GetEntity(part), shouldSeal);
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

        if (isSealed)
        {
            var partEv = new ModSuitSealedEvent();
            RaiseLocalEvent(ent.Owner, partEv);

            var suitEv = new ModSuitContainerPartSealedEvent(ent.Owner);
            RaiseLocalEvent(container, suitEv);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.SealSound, ent.Owner);
        }
        else
        {
            var partEv = new ModSuitUnsealedEvent();
            RaiseLocalEvent(ent.Owner, partEv);

            var suitEv = new ModSuitContainerPartUnsealedEvent(ent.Owner);
            RaiseLocalEvent(container, suitEv);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.UnsealSound, ent.Owner);
        }

        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, isSealed);
        UpdateUI(container);

        return true;
    }
}
