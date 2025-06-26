using Content.Server.Cargo.Systems;
using Content.Shared.Research.Systems;
using Content.Shared.Research.Components;
using Content.Shared.Cargo.Components;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;

namespace Content.Server.Research.Systems;

public sealed partial class ResearchEventSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    private const string FUND = "station-event-funding-";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchServerComponent, ResearchFundingEvent>(OnResearchFunding);
    }

    private void OnResearchFunding(EntityUid uid, ResearchServerComponent server, ResearchFundingEvent args)
    {
        var station = _station.GetOwningStation(args.Location) ?? EntityUid.Invalid; //get station Research Server is on
        if (TryComp<StationBankAccountComponent>(station, out var bank)) //check if there is a Station Bank Account
        {
            _cargo.UpdateBankAccount((station, bank), args.Payment, bank.RevenueDistribution);
        }
        else
        {
            Log.Debug("No Station Bank Account found, RandomResearchFunding could not proceed");
        }

        _chat.DispatchStationAnnouncement( //sent funding message
            station,  //Find in: Resources/Locale/en-US/station-events/funding.ftl
            Loc.GetString(args.Message, ("data1", Loc.GetString(args.Discipline))),
            playDefaultSound: true,
            colorOverride: Color.Gold
            );
    }
}
