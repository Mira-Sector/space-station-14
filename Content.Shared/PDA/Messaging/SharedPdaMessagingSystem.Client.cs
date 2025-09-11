using Content.Shared.CartridgeLoader;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Recipients;
using Content.Shared.Station;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<PdaMessagingClientComponent, MapInitEvent>(OnClientInit, after: [typeof(SharedStationSystem)]);
        SubscribeLocalEvent<PdaMessagingClientComponent, ComponentRemove>(OnClientRemove);

        SubscribeLocalEvent<PdaMessagingClientComponent, CartridgeAddedEvent>(OnClientAdded);

        SubscribeLocalEvent<PdaMessagingClientComponent, PdaOwnerChangedEvent>(OnClientPdaOwnerChanged);

        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageClientReceiveRecipientsEvent>(OnClientReceiveRecipients);
        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageSendMessageSourceEvent>(OnClientSendMessageSource);

        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageClientUpdateProfilePictureEvent>(OnClientUpdateProfilePicture);
        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageClientUpdateConnectedServerEvent>(OnClientUpdateConnectedServer);

        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageClientServerConnectedEvent>(OnClientServerConnected);
        SubscribeLocalEvent<PdaMessagingClientComponent, PdaMessageClientServerDisconnectedEvent>(OnClientServerDisconnected);

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

    private void OnClientAdded(Entity<PdaMessagingClientComponent> ent, ref CartridgeAddedEvent args)
    {
        UpdateClientProfileName(ent);
    }

    private void OnClientPdaOwnerChanged(Entity<PdaMessagingClientComponent> ent, ref PdaOwnerChangedEvent args)
    {
        UpdateClientProfileName(ent);
    }

    private void OnClientReceiveRecipients(Entity<PdaMessagingClientComponent> ent, ref PdaMessageClientReceiveRecipientsEvent args)
    {
        // no sending messages to yourself
        HashSet<BasePdaChatMessageable> recipients = [.. args.Recipients];
        foreach (var recipient in args.Recipients)
        {
            if (recipient.Id != ent.Comp.Profile.Id)
                continue;

            recipients.Remove(recipient);

            // update our own profile if needed
            ent.Comp.Profile = (PdaChatRecipientProfile)recipient;
            break;
        }

        ent.Comp.AvailableRecipients = recipients;
        Dirty(ent);
    }

    private void OnClientSendMessageSource(Entity<PdaMessagingClientComponent> ent, ref PdaMessageSendMessageSourceEvent args)
    {
        if (!args.Message.IsValid())
            return;

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

    private void OnClientUpdateProfilePicture(Entity<PdaMessagingClientComponent> ent, ref PdaMessageClientUpdateProfilePictureEvent args)
    {
        if (ent.Comp.Profile.Picture == args.ProfilePicture)
            return;

        ent.Comp.Profile.Picture = args.ProfilePicture;
        Dirty(ent);

        if (ent.Comp.Server is not { } server)
            return;

        var ev = new PdaMessageServerUpdateProfilePictureEvent(ent, args.ProfilePicture);
        RaiseLocalEvent(server, ref ev);
    }

    private void OnClientUpdateConnectedServer(Entity<PdaMessagingClientComponent> ent, ref PdaMessageClientUpdateConnectedServerEvent args)
    {
        var server = GetEntity(args.Server);
        if (ent.Comp.Server == server)
            return;

        // check if we can actually connect to it
        if (server != null)
        {
            if (_station.GetCurrentStation(ent.Owner) is not { } station)
                return;

            var availableServers = GetStationServersUid(station).ToHashSet();
            if (!availableServers.Contains(server.Value))
                return;
        }

        UpdateClientConnectedServer(ent!, server);
    }

    private void OnClientServerConnected(Entity<PdaMessagingClientComponent> ent, ref PdaMessageClientServerConnectedEvent args)
    {
    }

    private void OnClientServerDisconnected(Entity<PdaMessagingClientComponent> ent, ref PdaMessageClientServerDisconnectedEvent args)
    {
        ent.Comp.AvailableRecipients.Clear();
        Dirty(ent);
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

    private void UpdateClientProfileName(Entity<PdaMessagingClientComponent> ent)
    {
        var profilePicture = _profilePictures[ent.Comp.Profile.Picture];
        var newName = GetProfileName(ent, profilePicture);

        if (ent.Comp.Profile.Name == newName)
            return;

        ent.Comp.Profile.Name = newName;

        if (ent.Comp.Server is { } server)
        {
            var ev = new PdaMessageServerUpdateNameEvent(ent.Owner, newName);
            RaiseLocalEvent(server, ref ev);
        }
        else
        {
            Dirty(ent);
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

    public void UpdateClientConnectedServer(Entity<PdaMessagingClientComponent?> ent, EntityUid? newServer)
    {
        if (!ClientQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Server == newServer)
            return;

        var transferringServer = newServer != null;

        var oldServer = ent.Comp.Server;
        ent.Comp.Server = newServer;
        Dirty(ent);

        if (oldServer != null)
        {
            var serverDisconnectEv = new PdaMessageServerClientDisconnectedEvent(ent.Owner, ent.Comp.Profile);
            RaiseLocalEvent(oldServer.Value, ref serverDisconnectEv);

            var clientDisconnectEv = new PdaMessageClientServerDisconnectedEvent(oldServer.Value, transferringServer);
            RaiseLocalEvent(ent.Owner, ref clientDisconnectEv);
        }

        if (newServer != null)
        {
            var serverConnectEv = new PdaMessageServerClientConnectedEvent(ent.Owner, ent.Comp.Profile);
            RaiseLocalEvent(newServer.Value, ref serverConnectEv);

            var clientConnectEv = new PdaMessageClientServerConnectedEvent(newServer.Value, transferringServer);
            RaiseLocalEvent(ent.Owner, ref clientConnectEv);
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
