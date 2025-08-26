using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Recipients;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeClient()
    {
        SubscribeLocalEvent<PdaMessagingClientComponent, ComponentInit>(OnClientInit);

        SubscribeLocalEvent<PdaMessageNewServerAvailableEvent>(OnClientNewServerAvailable);
        SubscribeLocalEvent<PdaMessageNewProfileServerEvent>(OnClientNewProfileFromServer);
    }

    private void OnClientInit(Entity<PdaMessagingClientComponent> ent, ref ComponentInit args)
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

    private void OnClientNewServerAvailable(PdaMessageNewServerAvailableEvent args)
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

    private void OnClientNewProfileFromServer(PdaMessageNewProfileServerEvent args)
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
