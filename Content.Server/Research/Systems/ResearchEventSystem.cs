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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResearchServerComponent, ResearchFundingEvent>(OnResearchFunding);
    }

    private void Announce(EntityUid station, LocId announce, string data1 = "", string data2 = "", string data3 = "")
    {
        _chat.DispatchStationAnnouncement( //sent starting message.
            station,  //Find in: Resources/Locale/en-US/station-events/funding.ftl
            Loc.GetString(announce, ("data1", Loc.GetString(data1)), ("data2", Loc.GetString(data2)), ("data3", Loc.GetString(data3))),
            playDefaultSound: true,
            colorOverride: Color.Gold
            );
    }

    private void OnResearchFunding(EntityUid uid, ResearchServerComponent server, ResearchFundingEvent args)
    {
        var station = _station.GetOwningStation(args.Location) ?? EntityUid.Invalid;
        Log.Debug(ToPrettyString(station));

        if (TryComp<StationBankAccountComponent>(station, out var bank))
        {
            _cargo.UpdateBankAccount((station, bank), args.Payment, bank.RevenueDistribution);

            Log.Debug($"Deployed {args.Payment} Spesos");
        }
        else
        {
            Log.Debug("No Station Bank Account found, RandomResearchFunding could not proceed");
        }

        Announce(station, args.Message, args.Discipline);
    }
}
