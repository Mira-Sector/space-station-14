using Content.Server.Cargo.Systems;
using Content.Shared.Research.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Cargo.Components;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchEventSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchFundingEvent>(OnResearchFunding);
    }

    private void OnResearchFunding(ResearchFundingEvent args)
    {
        var station = _transform.GetGrid(args.Location.ToCoordinates()) ?? EntityUid.Invalid;
        Log.Debug(ToPrettyString(station));

        if (TryComp<StationBankAccountComponent>(station, out var bank))
        {
            _cargo.UpdateBankAccount((station, bank), args.Payment, bank.RevenueDistribution);

            Log.Debug($"Deployed {args.Payment} Spesos");
            //ResearchMessage() //separated from stationevent for now to reduce variables as to why subscribing doesn't work
        }
        else
        {
            Log.Debug("No Station Bank Account found, RandomResearchFunding could not proceed");
        }
    }
}
