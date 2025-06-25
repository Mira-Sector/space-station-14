using Content.Server.StationEvents.Components;
using Content.Shared.Research.Systems;
using Content.Server.Cargo.Systems;
using Content.Shared.Dataset;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Cargo.Components;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Research.Components;
using Content.Shared.Coordinates;

using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class RandomFundingRule : StationEventSystem<RandomFundingRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;


    protected override void Started(EntityUid uid, RandomFundingRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args); //does all the pre-amble in the base StationEventSystem

        if (!TryGetRandomStation(out var station)) //in theory the event-eligible target should always be the station, which has a bank account...
            return;

        // No station to deduct from.
        if (TryComp(station, out StationBankAccountComponent? bank))//...but if no bank account found cancel immediately.
        {
            var payment = (int)(component.BaseCash * (_random.Next(component.MaxMult) + 1)); //component-defined base cash multiplied by random number between 1 and component-defined maximum multiplier.
            var dep = "test"; //department suffix used for announcement, should always be replaced

            if (component.SplitFunds > _random.NextFloat()) //roll to see if funds are split or not. If true, funds are split between all departments.
            {
                _cargo.UpdateBankAccount((station.Value, bank), payment * 2, bank.RevenueDistribution); //split across 5 departments so make it a higher payment
                dep = "all"; //set department specifier to all for announcement
            }
            else
            {
                var b = bank.RevenueDistribution.ToList()[_random.Next(bank.RevenueDistribution.Count)]; //turn Revenue Distribution dictionary into a list, randomly choose one member of that list.
                _cargo.UpdateBankAccount((station.Value, bank), payment, b.Key); //if sending just a single department name the game assumes 100% of the cash goes to that department, no need for anything else.
                dep = b.Key; //also store the key for use in announcement
            }

            LocId data = "station-event-funding-" + dep.ToLower(); //create specific LocId string based off of department funding result
            ChatSystem.DispatchStationAnnouncement( //sent starting message.
                station.Value,  //Find in: Resources/Locale/en-US/station-events/funding.ftl
                Loc.GetString("station-event-funding-announcement", ("data", Loc.GetString(data)), ("reason", _random.Pick(_prototype.Index<LocalizedDatasetPrototype>("RandomFundingReason")))),
                playDefaultSound: false,
                colorOverride: Color.Gold
                );

        }
        else
        {
            Log.Debug("No Station Bank Account found, RandomFundingRule could not proceed");
        }
    }

    public void Fund() //make funding accessible through here so researchfundingevent can access it?
    {

    }

    public void ResearchMessage(EntityUid station, String? discipline)
    {
        LocId data = "station-event-funding-";
        if (discipline == null)
            data += "null";
        else
            data += discipline;

        ChatSystem.DispatchStationAnnouncement( //sent starting message.
            station,  //Find in: Resources/Locale/en-US/station-events/funding.ftl
            Loc.GetString("station-event-research-funding-announcement", ("data", Loc.GetString(data))),
            playDefaultSound: false,
            colorOverride: Color.Gold
            );
    }
}


