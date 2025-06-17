using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    private void InitializeDeployableRelay()
    {
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ModSuitGetUiEntriesEvent>(RelayToAllParts);
    }

    protected void RelayToDeployedParts<T>(Entity<ModSuitPartDeployableComponent> ent, ref T args)
    {
        var ev = new ModSuitDeployableRelayedEvent<T>(args, ent.Owner);
        RaiseEventToDeployedParts((ent.Owner, ent.Comp), ev);
    }

    protected void RelayToDeployableParts<T>(Entity<ModSuitPartDeployableComponent> ent, ref T args)
    {
        var ev = new ModSuitDeployableRelayedEvent<T>(args, ent.Owner);
        RaiseEventToDeployedParts((ent.Owner, ent.Comp), ev);
    }

    protected void RelayToAllParts<T>(Entity<ModSuitPartDeployableComponent> ent, ref T args)
    {
        var ev = new ModSuitDeployableRelayedEvent<T>(args, ent.Owner);
        RaiseEventToAllParts((ent.Owner, ent.Comp), ev);
    }

    protected void RelayToSuit<T>(Entity<ModSuitDeployedPartComponent> ent, ref T args)
    {
        var ev = new ModSuitDeployedPartRelayedEvent<T>(args, ent.Owner);
        RaiseEventToSuit((ent.Owner, ent.Comp), ev);
    }

    [PublicAPI]
    public void RaiseEventToDeployedParts<T>(Entity<ModSuitPartDeployableComponent?> ent, T ev) where T : class
    {
        foreach (var part in GetDeployedParts(ent))
            RaiseLocalEvent(part, ev);
    }

    [PublicAPI]
    public void RaiseEventToDeployedParts<T>(Entity<ModSuitPartDeployableComponent?> ent, ref T ev) where T : struct
    {
        foreach (var part in GetDeployedParts(ent))
            RaiseLocalEvent(part, ref ev);
    }

    [PublicAPI]
    public void RaiseEventToDeployableParts<T>(Entity<ModSuitPartDeployableComponent?> ent, T ev) where T : class
    {
        foreach (var part in GetDeployableParts(ent))
            RaiseLocalEvent(part, ev);
    }

    [PublicAPI]
    public void RaiseEventToDeployableParts<T>(Entity<ModSuitPartDeployableComponent?> ent, ref T ev) where T : struct
    {
        foreach (var part in GetDeployableParts(ent))
            RaiseLocalEvent(part, ref ev);
    }

    [PublicAPI]
    public void RaiseEventToAllParts<T>(Entity<ModSuitPartDeployableComponent?> ent, T ev) where T : class
    {
        foreach (var part in GetAllParts(ent))
            RaiseLocalEvent(part, ref ev);
    }

    [PublicAPI]
    public void RaiseEventToAllParts<T>(Entity<ModSuitPartDeployableComponent?> ent, ref T ev) where T : struct
    {
        foreach (var part in GetAllParts(ent))
            RaiseLocalEvent(part, ref ev);
    }

    [PublicAPI]
    public void RaiseEventToSuit<T>(Entity<ModSuitDeployedPartComponent?> ent, T ev) where T : class
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        RaiseLocalEvent(ent.Comp.Suit, ref ev);
    }

    [PublicAPI]
    public void RaiseEventToSuit<T>(Entity<ModSuitDeployedPartComponent?> ent, ref T ev) where T : struct
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        RaiseLocalEvent(ent.Comp.Suit, ref ev);
    }
}
