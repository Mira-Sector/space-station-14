using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Messages;
using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeHistory()
    {
    }

    public IEnumerable<IChatRecipient> GetSortedRecentlyMessaged(Entity<PdaMessagingHistoryComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        SortedDictionary<TimeSpan, List<IChatRecipient>> times = [];
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

    public IEnumerable<IChatMessage> GetHistory(Entity<PdaMessagingHistoryComponent?> ent, IChatRecipient recipient)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        if (!ent.Comp.Messages.TryGetValue(recipient, out var messages))
            yield break;

        var count = ent.Comp.MessageCount[recipient];

        for (var i = 0; i < count; i++)
            yield return messages[i];
    }
}
