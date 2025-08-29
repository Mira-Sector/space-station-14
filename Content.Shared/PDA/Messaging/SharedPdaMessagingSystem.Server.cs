using System.Linq;
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

        SubscribeLocalEvent<PdaMessagingServerComponent, PdaMessageClientConnectedEvent>(OnServerClientConnected);
        SubscribeLocalEvent<PdaMessagingServerComponent, PdaMessageClientDisconnectedEvent>(OnServerClientDisconnected);

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
                AddServerProfile(ent!, comp.Profile, uid);
        }
    }

    private void OnServerRemoved(Entity<PdaMessagingServerComponent> ent, ref ComponentRemove args)
    {
        var station = _station.GetCurrentStation(ent.Owner);

        var ev = new PdaMessageServerRemovedEvent(ent.Owner, station);
        RaiseLocalEvent(ref ev);
    }

    private void OnServerClientConnected(Entity<PdaMessagingServerComponent> ent, ref PdaMessageClientConnectedEvent args)
    {
        ent.Comp.Profiles[args.Profile] = args.Client;
        Dirty(ent);

        ServerUpdateConnectedClientsRecipients(ent);
    }

    private void OnServerClientDisconnected(Entity<PdaMessagingServerComponent> ent, ref PdaMessageClientDisconnectedEvent args)
    {
        ent.Comp.Profiles[args.Profile] = null;
        Dirty(ent);

        ServerUpdateConnectedClientsRecipients(ent);
    }

    private void OnServerNewProfileFromClient(ref PdaMessageNewProfileClientEvent args)
    {
        if (_station.GetCurrentStation(args.Client) is not { } station)
            return;

        foreach (var server in GetStationServers(station))
            AddServerProfile(server!, args.Profile, args.Client);
    }

    private void ServerUpdateConnectedClientsRecipients(Entity<PdaMessagingServerComponent> ent)
    {
        var recipients = GetServerRecipients(ent!).ToHashSet();
        var ev = new PdaMessageClientReceiveRecipientsEvent(recipients);

        foreach (var (_, profileClient) in ent.Comp.Profiles)
        {
            if (profileClient is not { } client)
                continue;

            RaiseLocalEvent(client, ref ev);
        }
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

    public IEnumerable<BasePdaChatMessageable> GetServerRecipients(Entity<PdaMessagingServerComponent?> ent)
    {
        if (!ServerQuery.Resolve(ent.Owner, ref ent.Comp))
            yield break;

        foreach (var (profile, _) in ent.Comp.Profiles)
            yield return profile;
    }

    public void AddServerProfile(Entity<PdaMessagingServerComponent?> ent, PdaChatRecipientProfile profile, EntityUid? client)
    {
        if (!ServerQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Profiles.ContainsKey(profile))
            return;

        ent.Comp.Profiles[profile] = client;
        Dirty(ent);

        var ev = new PdaMessageNewProfileServerEvent(ent.Owner, profile, client);
        RaiseLocalEvent(ref ev);
    }
}
