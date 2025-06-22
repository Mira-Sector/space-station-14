using Content.Server.StationEvents.Components;
using Content.Server.Station.Systems;
using Content.Server.Cargo.Systems;
using Content.Shared.Dataset;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Cargo.Systems;
using Content.Shared.Cargo.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class RandomFundingRule : StationEventSystem<RandomFundingRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    protected override void Started(EntityUid uid, RandomFundingRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        //var station = _station.GetOwningStation(uid);
        //if (station == null)
        //    return;
        // No station to deduct from.
        if (TryComp(station, out StationBankAccountComponent? bank))
        {
            var payment = (int)(component.BaseCash * (_random.Next(component.MaxMult) + 1));
            var dep = "test"; //should always be replaced
            if (component.SplitFunds > _random.NextFloat()) //roll to see if funds are split or not. If true, funds are split between all departments.
            {
                _cargo.UpdateBankAccount((station.Value, bank), payment * 2, bank.RevenueDistribution); //split across 5 departments so make it a higher value
                dep = "all";
            }
            else
            {
                var b = bank.RevenueDistribution.ToList()[_random.Next(bank.RevenueDistribution.Count)];
                _cargo.UpdateBankAccount((station.Value, bank), payment, b.Key);
                dep = b.Key;
            }

            //dep = dep.ToLower();
            LocId data = "station-event-funding-" + dep.ToLower();
            ChatSystem.DispatchStationAnnouncement(
            station.Value,
            Loc.GetString("station-event-funding-announcement", ("data", Loc.GetString(data)), ("reason", _random.Pick(_prototype.Index<LocalizedDatasetPrototype>("RandomFundingReason")))),
            playDefaultSound: false,
            colorOverride: Color.Gold
            );

        }
        else
        {
            Log.Debug("No Station Bank Account found");
        }
    }
}
