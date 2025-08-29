using System.Linq;
using Content.Shared.PDA.Messaging;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;

namespace Content.Server.PDA.Messaging;

public sealed partial class PdaMessagingSystem : SharedPdaMessagingSystem
{
    private void InitializeServer()
    {
        SubscribeLocalEvent<PdaMessagingServerComponent, PdaMessageSentMessageServerEvent>(OnServerMessageSentServer);
    }

    // server side so client cant snoop
    private void OnServerMessageSentServer(Entity<PdaMessagingServerComponent> ent, ref PdaMessageSentMessageServerEvent args)
    {
        var source = ent.Comp.Profiles[args.Message.Sender]!.Value;

        var recipients = args.Message.Recipient.GetRecipients().ToHashSet();
        recipients.Remove(args.Message.Sender); // sender has their own events
        foreach (var recipient in recipients)
        {
            if (!ent.Comp.Profiles.TryGetValue(recipient, out var recipientClient) || recipientClient is not { } client)
                return;

            var serverAttemptEv = new PdaMessageReplicateAttemptServerEvent(client, source, args.Message);
            RaiseLocalEvent(ent.Owner, serverAttemptEv);
            if (serverAttemptEv.Cancelled)
                continue;

            var clientAttemptEv = new PdaMessageReplicateAttemptClientEvent(ent.Owner, source, args.Message);
            RaiseLocalEvent(client, clientAttemptEv);
            if (clientAttemptEv.Cancelled)
                continue;

            // no source event as it has already been sent to the server
            // cancel sending it to the server instead

            var serverEv = new PdaMessageReplicatedMessageServerEvent(client, source, args.Message);
            RaiseLocalEvent(ent.Owner, ref serverEv);

            var clientEv = new PdaMessageReplicatedMessageClientEvent(ent.Owner, source, args.Message);
            RaiseLocalEvent(client, ref clientEv);

            var sourceEv = new PdaMessageReplicatedMessageSourceEvent(client, ent.Owner, args.Message);
            RaiseLocalEvent(source, ref sourceEv);
        }
    }
}
