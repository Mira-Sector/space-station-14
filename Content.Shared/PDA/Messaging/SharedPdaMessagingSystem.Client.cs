using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Recipients;
using Content.Shared.Station;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<PdaMessagingClientComponent, MapInitEvent>(OnClientInit, after: [typeof(SharedStationSystem)]);

        SubscribeLocalEvent<PdaMessageNewServerAvailableEvent>(OnClientNewServerAvailable);
        SubscribeLocalEvent<PdaMessageServerRemovedEvent>(OnClientServerRemoved);
        SubscribeLocalEvent<PdaMessageNewProfileServerEvent>(OnClientNewProfileFromServer);
    }

    private void OnClientInit(Entity<PdaMessagingClientComponent> ent, ref MapInitEvent args)
    {
        // if a server already exists provide a sane default
        // so the user isnt baffled why it isnt working
        if (_station.GetCurrentStation(ent.Owner) is { } station)
        {
            var servers = GetStationServers(station);
            ent.Comp.Server = servers.FirstOrNull();
        }

        UpdateClientProfile(ent!, GetDefaultProfile(ent));
    }

    private void OnClientRemoved(Entity<PdaMessagingClientComponent> ent, ref ComponentRemove args)
    {
    }

    private void OnClientNewServerAvailable(ref PdaMessageNewServerAvailableEvent args)
    {
        var query = EntityQueryEnumerator<PdaMessagingClientComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // we only want to provide the initial server
            // anything after this is up to the user input to decide
            if (comp.Server != null)
                continue;

            comp.Server = args.Server;
            Dirty(uid, comp);
        }
    }

    private void OnClientServerRemoved(ref PdaMessageServerRemovedEvent args)
    {
        var query = EntityQueryEnumerator<PdaMessagingClientComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Server != args.Server)
                continue;

            if (_station.GetCurrentStation(uid) is not { } station)
                continue;

            // try and provide a new server if it exists
            var servers = GetStationServers(station);
            comp.Server = servers.FirstOrNull();
        }
    }

    private void OnClientNewProfileFromServer(ref PdaMessageNewProfileServerEvent args)
    {
        var query = EntityQueryEnumerator<PdaMessagingClientComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Server == args.Server)
                AddClientRecipient((uid, comp), args.Profile);
        }
    }

    public void UpdateClientProfile(Entity<PdaMessagingClientComponent?> ent, ChatRecipientProfile profile)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Profile = profile;
        Dirty(ent);

        var ev = new PdaMessageNewProfileClientEvent(ent.Owner, profile);
        RaiseLocalEvent(ref ev);
    }

    public void AddClientRecipient(Entity<PdaMessagingClientComponent?> ent, IChatRecipient recipient)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.AvailableRecipients.Contains(recipient))
            return;

        ent.Comp.AvailableRecipients.Add(recipient);
        Dirty(ent);
    }
}
