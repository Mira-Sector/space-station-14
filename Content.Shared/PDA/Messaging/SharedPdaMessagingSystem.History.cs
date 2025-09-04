using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Events;
using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeHistory()
    {
        SubscribeLocalEvent<PdaMessagingHistoryComponent, PdaMessageSentMessageSourceEvent>(OnHistoryMessageSentSource);
        SubscribeLocalEvent<PdaMessagingHistoryComponent, PdaMessageSentMessageServerEvent>(OnHistoryMessageSentServer);
        SubscribeLocalEvent<PdaMessagingHistoryComponent, PdaMessageReplicatedMessageClientEvent>(OnHistoryMessageReplicatedClient);
    }

    private void OnHistoryMessageSentSource(Entity<PdaMessagingHistoryComponent> ent, ref PdaMessageSentMessageSourceEvent args)
    {
        UpdateHistory(ent, args.Message, args.Message.Recipient);
    }

    private void OnHistoryMessageSentServer(Entity<PdaMessagingHistoryComponent> ent, ref PdaMessageSentMessageServerEvent args)
    {
        UpdateHistory(ent, args.Message, args.Message.Recipient);
    }

    private void OnHistoryMessageReplicatedClient(Entity<PdaMessagingHistoryComponent> ent, ref PdaMessageReplicatedMessageClientEvent args)
    {
        var toUpdate = args.Message.Recipient.GetRecipientMessageable(args.Message);
        UpdateHistory(ent, args.Message, toUpdate);
    }

    private void UpdateHistory(Entity<PdaMessagingHistoryComponent> ent, BasePdaChatMessage message, BasePdaChatMessageable toUpdate)
    {
        if (!ent.Comp.Messages.TryGetValue(toUpdate, out var messages))
        {
            messages = new BasePdaChatMessage[ent.Comp.MaxHistory];
            ent.Comp.Messages[toUpdate] = messages;
            ent.Comp.MessageCount[toUpdate] = 0;
            ent.Comp.MessageIndex[toUpdate] = 0;
        }

        var index = ent.Comp.MessageIndex[toUpdate];
        var count = ent.Comp.MessageCount[toUpdate];

        messages[index] = message;
        ent.Comp.MessageIndex[toUpdate] = (index + 1) % ent.Comp.MaxHistory;

        if (count < ent.Comp.MaxHistory)
            ent.Comp.MessageCount[toUpdate] = count + 1;

        ent.Comp.LastMessage[toUpdate] = message.SentAt;
        Dirty(ent);
    }

    public IEnumerable<BasePdaChatMessageable> GetSortedRecentlyMessaged(Entity<PdaMessagingHistoryComponent?> ent)
    {
        if (!HistoryQuery.Resolve(ent.Owner, ref ent.Comp))
            yield break;

        SortedDictionary<TimeSpan, List<BasePdaChatMessageable>> times = [];
        foreach (var (recipient, time) in ent.Comp.LastMessage)
        {
            if (!times.TryGetValue(time, out var recipients))
                recipients = [];

            recipients.Add(recipient);
            times[time] = recipients;
        }

        foreach (var (_, recipients) in times)
        {
            foreach (var recipient in recipients)
                yield return recipient;
        }
    }

    public IEnumerable<BasePdaChatMessage> GetHistory(Entity<PdaMessagingHistoryComponent?> ent, BasePdaChatMessageable recipient)
    {
        if (!HistoryQuery.Resolve(ent.Owner, ref ent.Comp))
            yield break;

        if (!ent.Comp.Messages.TryGetValue(recipient, out var messages))
            yield break;

        var count = ent.Comp.MessageCount[recipient];
        var index = ent.Comp.MessageIndex[recipient];

        for (var i = 0; i < count; i++)
        {
            var messageIndex = (index - count + i + ent.Comp.MaxHistory) % ent.Comp.MaxHistory;
            yield return messages[messageIndex];
        }
    }
}
