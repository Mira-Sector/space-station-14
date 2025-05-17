using Content.Shared.Modules.ModSuit.Components;
using Content.Shared.Modules.ModSuit.Events;
using JetBrains.Annotations;

namespace Content.Shared.Modules.ModSuit;

public partial class SharedModSuitSystem
{
    private void InitializeDeployableRelay()
    {
        SubscribeLocalEvent<ModSuitPartDeployableComponent, ModSuitGetUiStatesEvent>(RelayToDeployableParts);
    }

    private void RelayToDeployedParts<T>(Entity<ModSuitPartDeployableComponent> ent, ref T args)
    {
        var ev = new ModSuitDeployableRelayedEvent<T>(args, ent.Owner);
        RaiseEventToDeployedParts((ent.Owner, ent.Comp), ev);
    }

    private void RelayToDeployableParts<T>(Entity<ModSuitPartDeployableComponent> ent, ref T args)
    {
        var ev = new ModSuitDeployableRelayedEvent<T>(args, ent.Owner);
        RaiseEventToDeployedParts((ent.Owner, ent.Comp), ev);
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
}
