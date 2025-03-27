using Content.Server.Radio.EntitySystems;
using Content.Server.Supermatter.Components;
using Content.Server.Supermatter.Events;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server.Supermatter;

public sealed partial class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterIntegerityComponent, ComponentInit>(OnIntegerityInit);
        SubscribeLocalEvent<SupermatterGasReactionComponent, AtmosExposedUpdateEvent>(OnAtmosExposed);
        SubscribeLocalEvent<SupermatterRadioComponent, SupermatterIntegerityModifiedEvent>(OnRadioIntegerityModified);
    }

    private void OnIntegerityInit(Entity<SupermatterIntegerityComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Integerity = ent.Comp.MaxIntegrity;
    }

    private void OnAtmosExposed(Entity<SupermatterGasReactionComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        if (args.GasMixture.TotalMoles < Atmospherics.GasMinMoles)
            return;

        foreach (var (gas, reaction) in ent.Comp.GasReactions)
        {
            DoGasReaction(gas, reaction, args.GasMixture);
        }
    }

    private void DoGasReaction(Gas gas, SupermatterGasReaction reaction, GasMixture air)
    {
    }

    private void OnRadioIntegerityModified(Entity<SupermatterRadioComponent> ent, ref SupermatterIntegerityModifiedEvent args)
    {
        var positive = args.CurrentIntegerity - args.PreviousIntegerity > 0;
        KeyValuePair<FixedPoint2, LocId> match = new();

        foreach (var (threshold, message) in ent.Comp.Messages)
        {
            if (positive)
            {
                if (threshold > args.CurrentIntegerity)
                    continue;
            }
            else
            {
                if (threshold < args.CurrentIntegerity)
                    continue;
            }

            if (threshold > match.Key)
                match = new(threshold, message);
        }

        if (match.Key == ent.Comp.LastMessage)
            return;

        ent.Comp.LastMessage = match.Key;
        _radio.SendRadioMessage(ent, Loc.GetString(match.Value), ent.Comp.Channel, ent);
    }

    public void ModifyIntegerity(Entity<SupermatterIntegerityComponent?> ent, FixedPoint2 integerity)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldIntegerity = ent.Comp.Integerity;
        ent.Comp.Integerity += integerity;

        var modifiedEv = new SupermatterIntegerityModifiedEvent(ent.Comp.Integerity, oldIntegerity);
        RaiseLocalEvent(ent, modifiedEv);

        if (ent.Comp.Integerity < 0)
        {
            var delaminatingEv = new SupermatterDelaminatedEvent();
            RaiseLocalEvent(ent, delaminatingEv);
            ent.Comp.Integerity = 0f; //clamp it
        }
    }

    public void ModifyEnergy(Entity<SupermatterEnergyComponent?> ent, float energy)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        var oldEnergy = ent.Comp.CurrentEnergy;
        ent.Comp.CurrentEnergy += energy;

        var modifiedEv = new SupermatterEnergyModifiedEvent(ent.Comp.CurrentEnergy, oldEnergy);
        RaiseLocalEvent(ent, modifiedEv);
    }
}
