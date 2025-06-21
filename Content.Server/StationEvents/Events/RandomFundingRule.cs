using Content.Server.StationEvents.Components;
using Content.Server.Station.Systems;
using Content.Server.Cargo.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Cargo.Systems;
using Content.Shared.Cargo.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Events;

public sealed class RandomFundingRule : StationEventSystem<RandomFundingRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;

    protected override void Started(EntityUid uid, RandomFundingRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        //if (!TryGetRandomStation(out var chosenStation))
        //    return;

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;
        // No station to deduct from.
        if (TryComp(station, out StationBankAccountComponent? bank))
        {
            if (component.SplitFunds > _random.NextFloat()) //roll to see if funds are split or not. If true, funds are split between all departments.
            {
                _cargo.UpdateBankAccount((station.Value, bank), component.BaseCash, bank.RevenueDistribution);
            }

        }
    }
}
