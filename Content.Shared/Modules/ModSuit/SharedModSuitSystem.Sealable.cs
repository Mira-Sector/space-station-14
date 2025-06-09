using System.Diagnostics.CodeAnalysis;
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

        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotEquippedEvent>(OnSealableEquipped);
        SubscribeLocalEvent<ModSuitSealableComponent, ClothingGotUnequippedEvent>(OnSealableUnequipped);

        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitDeployablePartUnequippedEvent>(OnSealableDeployablePartUnequipped);

        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitGetUiEntriesEvent>(OnSealableGetUiEntries);
        SubscribeLocalEvent<ModSuitSealableComponent, ModSuitDeployableRelayedEvent<ModSuitGetUiEntriesEvent>>((u, c, a) => OnSealableGetUiEntries((u, c), ref a.Args));

        SubscribeAllEvent<ModSuitSealButtonMessage>(OnSealableUiButton);
    }

    private void UpdateSealable(float frameTime)
    {
        var query = EntityQueryEnumerator<ModSuitSealableComponent, ModSuitSealablePendingComponent>();
        while (query.MoveNext(out var uid, out var sealable, out var pending))
        {
            if (pending.NextUpdate > _timing.CurTime)
                continue;

            SetSeal((uid, sealable), pending.ShouldSeal);
            RemCompDeferred<ModSuitSealablePendingComponent>(uid);
        }
    }

    private void OnSealableInit(Entity<ModSuitSealableComponent> ent, ref ComponentInit args)
    {
        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, ent.Comp.Sealed);
    }

    private void OnSealableEquipped(Entity<ModSuitSealableComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Wearer;
        Dirty(ent);
    }

    private void OnSealableUnequipped(Entity<ModSuitSealableComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
        Dirty(ent);

        // handled in a separate event
        if (HasComp<ModSuitDeployedPartComponent>(ent.Owner))
            return;

        // cant be sealed when not worn
        SetSeal((ent.Owner, ent.Comp), false);
    }


    private void OnSealableDeployablePartUnequipped(Entity<ModSuitSealableComponent> ent, ref ModSuitDeployablePartUnequippedEvent args)
    {
        SetSeal((ent.Owner, ent.Comp), false, args.PartNumber);
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
        var i = 0;
        foreach (var (part, shouldSeal) in args.Parts)
            SetSeal(GetEntity(part), shouldSeal, i++);
    }

    [PublicAPI]
    public bool IsSealed(Entity<ModSuitSealableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        return ent.Comp.Sealed;
    }

    [PublicAPI]
    public bool SetSeal(Entity<ModSuitSealableComponent?> ent, bool shouldSeal, int sealedPartCount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // prevent sending events and excessive dirtying
        if (ent.Comp.Sealed == shouldSeal)
            return true;

        if (!IsDelayed((ent.Owner, ent.Comp), sealedPartCount, out var delay))
            return SetSeal(ent, shouldSeal);

        EnsureComp<ModSuitSealablePendingComponent>(ent.Owner, out var pendingComp);
        pendingComp.NextUpdate = _timing.CurTime + delay.Value;
        pendingComp.ShouldSeal = shouldSeal;
        Dirty(ent.Owner, pendingComp);
        return true;
    }

    [PublicAPI]
    public bool SetSeal(Entity<ModSuitSealableComponent?> ent, bool shouldSeal)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        // prevent sending events and excessive dirtying
        if (ent.Comp.Sealed == shouldSeal)
            return true;

        ent.Comp.Sealed = shouldSeal;
        RemCompDeferred<ModSuitSealablePendingComponent>(ent.Owner); // stop any pending updates overwriting us
        Dirty(ent);

        var container = CompOrNull<ModSuitDeployedPartComponent>(ent.Owner)?.Suit ?? ent.Owner;

        if (shouldSeal)
        {
            var partEv = new ModSuitSealedEvent(ent.Comp.Wearer);
            RaiseLocalEvent(ent.Owner, partEv);

            var suitEv = new ModSuitContainerPartSealedEvent(ent.Owner);
            RaiseLocalEvent(container, suitEv);

            if (ent.Comp.SealedComponents != null)
                EntityManager.AddComponents(ent.Owner, ent.Comp.SealedComponents, true);
        }
        else
        {
            var partEv = new ModSuitUnsealedEvent(ent.Comp.Wearer);
            RaiseLocalEvent(ent.Owner, partEv);

            var suitEv = new ModSuitContainerPartUnsealedEvent(ent.Owner);
            RaiseLocalEvent(container, suitEv);

            if (ent.Comp.SealedComponents != null)
                EntityManager.RemoveComponents(ent.Owner, ent.Comp.SealedComponents);
        }

        PlaySound((ent.Owner, ent.Comp), shouldSeal);

        Appearance.SetData(ent.Owner, ModSuitSealedVisuals.Sealed, shouldSeal);
        UpdateUI(container);

        return true;
    }

    private void PlaySound(Entity<ModSuitSealableComponent> ent, bool shouldSeal)
    {
        if (!_net.IsServer)
            return;

        if (shouldSeal)
            _audio.PlayPvs(ent.Comp.SealSound, ent.Owner);
        else
            _audio.PlayPvs(ent.Comp.UnsealSound, ent.Owner);
    }

    private static bool IsDelayed(Entity<ModSuitSealableComponent> ent, int sealedPartCount, [NotNullWhen(true)] out TimeSpan? delay)
    {
        delay = null;

        if (ent.Comp.DelayPerPart is not { } delayPerPart)
            return false;

        if (sealedPartCount > 0)
        {
            delay = delayPerPart * sealedPartCount;
            return true;
        }

        return false;
    }
}
