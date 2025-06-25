using Content.Server.Cargo.Systems;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Coordinates;

using Content.Shared.Cargo.Components;
using Content.Shared.Dataset;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;

using Robust.Shared.Prototypes;

namespace Content.Server.Research.Systems;
public sealed partial class ResearchEventSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    //[Dependency] private readonly StationEventSystem<RandomFundingRuleComponent> _station = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchFundingEvent>(OnResearchFunding);

        Log.Debug("Subscribed to Event");
    }

    private void OnResearchFunding(ResearchFundingEvent args)
    {
        Log.Debug("RECEIVED");

        var station = _transform.GetGrid(args.Location.ToCoordinates()) ?? EntityUid.Invalid;
        Log.Debug(station.Id.ToString());
        if (TryComp(station, out StationBankAccountComponent? bank))
        {
            _cargo.UpdateBankAccount((station, bank), args.Payment, bank.RevenueDistribution);

            var discipline = "null";
            if (args.Discipline != null)
                discipline = args.Discipline;

            Log.Debug($"Deployed {args.Payment} Spesos");
            //ResearchMessage() //seperated from stationevent for now to reduce variables as to why subscribing doesn't work
        }
        else
        {
            Log.Debug("No Station Bank Account found, RandomResearchFunding could not proceed");
        }
    }
}
