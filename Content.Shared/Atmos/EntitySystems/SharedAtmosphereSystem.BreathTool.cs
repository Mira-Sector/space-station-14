using Content.Shared.Atmos.Components;
using Content.Shared.Clothing;
using Content.Shared.Modules.ModSuit.Events;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedAtmosphereSystem
{
    private void InitializeBreathTool()
    {
        SubscribeLocalEvent<BreathToolComponent, ComponentShutdown>(OnBreathToolShutdown);
        SubscribeLocalEvent<BreathToolComponent, ItemMaskToggledEvent>(OnMaskToggled);

        SubscribeLocalEvent<BreathToolComponent, ModSuitSealedEvent>(OnSealed);
        SubscribeLocalEvent<BreathToolComponent, ModSuitUnsealedEvent>(OnUnsealed);
    }

    private void OnBreathToolShutdown(Entity<BreathToolComponent> entity, ref ComponentShutdown args)
    {
        DisconnectInternals(entity);
    }

    public void DisconnectInternals(Entity<BreathToolComponent> entity, bool forced = false)
    {
        var old = entity.Comp.ConnectedInternalsEntity;

        if (old == null)
            return;

        entity.Comp.ConnectedInternalsEntity = null;

        if (_internalsQuery.TryComp(old, out var internalsComponent))
        {
            _internals.DisconnectBreathTool((old.Value, internalsComponent), entity.Owner, forced: forced);
        }

        Dirty(entity);
    }

    private void OnMaskToggled(Entity<BreathToolComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (args.Mask.Comp.IsToggled)
        {
            DisconnectInternals(ent, forced: true);
        }
        else
        {
            if (_internalsQuery.TryComp(args.Wearer, out var internals))
            {
                _internals.ConnectBreathTool((args.Wearer.Value, internals), ent);
            }
        }
    }

    private void OnSealed(Entity<BreathToolComponent> ent, ref ModSuitSealedEvent args)
    {
        if (_internalsQuery.TryComp(args.Wearer, out var internals))
            _internals.ConnectBreathTool((args.Wearer.Value, internals), ent);
    }

    private void OnUnsealed(Entity<BreathToolComponent> ent, ref ModSuitUnsealedEvent args)
    {
        DisconnectInternals(ent, forced: true);
    }
}
