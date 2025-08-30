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
        SubscribeLocalEvent<PdaMessagingClientComponent, ComponentRemove>(OnClientRemove);

        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageClientReceiveRecipientsEvent>(OnClientReceiveRecipients);
        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageSendMessageSourceEvent>(OnClientSendMessageSource);

        SubscribeLocalEvent<PdaMessageNewServerAvailableEvent>(OnClientNewServerAvailable);
        SubscribeLocalEvent<PdaMessageServerRemovedEvent>(OnClientServerRemoved);
        SubscribeLocalEvent<PdaMessageNewProfileServerEvent>(OnClientNewProfileFromServer);
    }

    private void OnClientInit(Entity<PdaMessagingClientComponent> ent, ref MapInitEvent args)
    {
        var profile = GetDefaultProfile(ent);
        UpdateClientProfile(ent!, profile);

        // if a server already exists provide a sane default
        // so the user isnt baffled why it isnt working
        if (_station.GetCurrentStation(ent.Owner) is { } station)
        {
            var servers = GetStationServers(station);
            UpdateClientConnectedServer(ent!, servers.FirstOrNull());
        }
    }

    private void OnClientRemove(Entity<PdaMessagingClientComponent> ent, ref ComponentRemove args)
    {
        UpdateClientConnectedServer(ent!, null);
    }

    private void OnClientReceiveRecipients(Entity<PdaMessagingClientComponent> ent, ref PdaMessageClientReceiveRecipientsEvent args)
    {
        // no sending messages to yourself
        HashSet<BasePdaChatMessageable> recipients = [.. args.Recipients];
        recipients.Remove(ent.Comp.Profile);

        ent.Comp.AvailableRecipients = recipients;
        Dirty(ent);
    }

    private void OnClientSendMessageSource(Entity<PdaMessagingClientComponent> ent, ref PdaMessageSendMessageSourceEvent args)
    {
        if (ent.Comp.Server is not { } server)
            return;

        var sourceAttemptEv = new PdaMessageSendAttemptSourceEvent(server, args.Message);
        RaiseLocalEvent(ent.Owner, sourceAttemptEv);
        if (sourceAttemptEv.Cancelled)
            return;

        var serverAttemptEv = new PdaMessageSendAttemptServerEvent(ent, args.Message);
        RaiseLocalEvent(server, serverAttemptEv);
        if (serverAttemptEv.Cancelled)
            return;

        var sourceEv = new PdaMessageSentMessageSourceEvent(server, args.Message);
        RaiseLocalEvent(ent.Owner, ref sourceEv);

        var serverEv = new PdaMessageSentMessageServerEvent(ent, args.Message);
        RaiseLocalEvent(server, ref serverEv);
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

            UpdateClientConnectedServer((uid, comp), args.Server);
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
            UpdateClientConnectedServer((uid, comp), servers.FirstOrNull());
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

    public void UpdateClientProfile(Entity<PdaMessagingClientComponent?> ent, PdaChatRecipientProfile profile)
    {
        if (!ClientQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        ent.Comp.Profile = profile;
        Dirty(ent);

        var ev = new PdaMessageNewProfileClientEvent(ent.Owner, profile);
        RaiseLocalEvent(ref ev);
    }

    public void UpdateClientConnectedServer(Entity<PdaMessagingClientComponent?> ent, EntityUid? server)
    {
        if (!ClientQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Server == server)
            return;

        if (ent.Comp.Server is { } oldServer)
        {
            var disconnectEv = new PdaMessageClientDisconnectedEvent(ent.Owner, ent.Comp.Profile);
            RaiseLocalEvent(oldServer, ref disconnectEv);
        }

        ent.Comp.Server = server;
        Dirty(ent);

        if (server is { } newServer)
        {
            var connectEv = new PdaMessageClientConnectedEvent(ent.Owner, ent.Comp.Profile);
            RaiseLocalEvent(newServer, ref connectEv);
        }
    }

    public void AddClientRecipient(Entity<PdaMessagingClientComponent?> ent, BasePdaChatMessageable recipient)
    {
        if (!ClientQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.AvailableRecipients.Contains(recipient))
            return;

        ent.Comp.AvailableRecipients.Add(recipient);
        Dirty(ent);
    }

    public IEnumerable<BasePdaChatMessageable> GetClientRecipients(Entity<PdaMessagingClientComponent?, PdaMessagingHistoryComponent?> ent)
    {
        if (!ClientQuery.Resolve(ent.Owner, ref ent.Comp1))
            yield break;

        if (HistoryQuery.Resolve(ent.Owner, ref ent.Comp2, false))
        {
            var remaining = ent.Comp1.AvailableRecipients;

            // add our most recently messaged first
            var recent = GetSortedRecentlyMessaged((ent.Owner, ent.Comp2));
            foreach (var recipient in recent)
            {
                yield return recipient;
                remaining.Remove(recipient);
            }

            // add any contacts with no messages at the end with a radnom order
            foreach (var recipient in remaining)
                yield return recipient;
        }
        else
        {
            foreach (var recipient in ent.Comp1.AvailableRecipients)
                yield return recipient;
        }
    }
}
