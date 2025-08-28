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
    }

    private void OnHistoryMessageSentSource(Entity<PdaMessagingHistoryComponent> ent, ref PdaMessageSentMessageSourceEvent args)
    {
        UpdateHistory(ent, args.Message);
    }

    private void OnHistoryMessageSentServer(Entity<PdaMessagingHistoryComponent> ent, ref PdaMessageSentMessageServerEvent args)
    {
        UpdateHistory(ent, args.Message);
    }

    private void UpdateHistory(Entity<PdaMessagingHistoryComponent> ent, BasePdaChatMessage message)
    {
        if (!ent.Comp.Messages.TryGetValue(message.Recipient, out var messages))
        {
            messages = new BasePdaChatMessage[ent.Comp.MaxHistory];
            ent.Comp.Messages[message.Recipient] = messages;
            ent.Comp.MessageCount[message.Recipient] = 0;
            ent.Comp.MessageIndex[message.Recipient] = 0;
        }

        var index = ent.Comp.MessageIndex[message.Recipient];
        var count = ent.Comp.MessageCount[message.Recipient];

        messages[index] = message;
        ent.Comp.MessageIndex[message.Recipient] = (index + 1) % ent.Comp.MaxHistory;

        if (count < ent.Comp.MaxHistory)
            ent.Comp.MessageCount[message.Recipient] = count + 1;

        ent.Comp.LastMessage[message.Recipient] = _timing.CurTime;
        Dirty(ent);
    }

    public IEnumerable<IPdaChatRecipient> GetSortedRecentlyMessaged(Entity<PdaMessagingHistoryComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        SortedDictionary<TimeSpan, List<IPdaChatRecipient>> times = [];
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

    public IEnumerable<BasePdaChatMessage> GetHistory(Entity<PdaMessagingHistoryComponent?> ent, IPdaChatRecipient recipient)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
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
