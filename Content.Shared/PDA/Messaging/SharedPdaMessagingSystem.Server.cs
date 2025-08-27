using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Recipients;
using Content.Shared.Station;
using Content.Shared.Station.Components;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<PdaMessagingServerComponent, MapInitEvent>(OnServerInit, after: [typeof(SharedStationSystem)]);
        SubscribeLocalEvent<PdaMessagingServerComponent, ComponentRemove>(OnServerRemoved);

        SubscribeLocalEvent<PdaMessageNewProfileClientEvent>(OnServerNewProfileFromClient);
    }

    private void OnServerInit(Entity<PdaMessagingServerComponent> ent, ref MapInitEvent args)
    {
        var station = _station.GetCurrentStation(ent.Owner);

        var ev = new PdaMessageNewServerAvailableEvent(ent.Owner, station);
        RaiseLocalEvent(ref ev);

        var query = EntityQueryEnumerator<PdaMessagingClientComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Server == ent.Owner)
                AddServerProfile(ent!, comp.Profile);
        }
    }

    private void OnServerRemoved(Entity<PdaMessagingServerComponent> ent, ref ComponentRemove args)
    {
        var station = _station.GetCurrentStation(ent.Owner);

        var ev = new PdaMessageServerRemovedEvent(ent.Owner, station);
        RaiseLocalEvent(ref ev);
    }

    private void OnServerNewProfileFromClient(ref PdaMessageNewProfileClientEvent args)
    {
        if (_station.GetCurrentStation(args.Client) is not { } station)
            return;

        foreach (var server in GetStationServers(station))
            AddServerProfile(server!, args.Profile);
    }

    public IEnumerable<Entity<PdaMessagingServerComponent>> GetStationServers(EntityUid station)
    {
        var query = EntityQueryEnumerator<PdaMessagingServerComponent, StationTrackerComponent>();
        while (query.MoveNext(out var uid, out var comp, out var tracker))
        {
            if (_station.GetCurrentStation((uid, tracker)) == station)
                yield return (uid, comp);
        }
    }

    public void AddServerProfile(Entity<PdaMessagingServerComponent?> ent, ChatRecipientProfile profile)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Profiles.Contains(profile))
            return;

        ent.Comp.Profiles.Add(profile);
        Dirty(ent);

        var ev = new PdaMessageNewProfileServerEvent(ent.Owner, profile);
        RaiseLocalEvent(ref ev);
    }
}
