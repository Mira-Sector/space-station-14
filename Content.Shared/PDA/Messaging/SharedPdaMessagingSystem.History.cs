using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Messages;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    private void InitializeHistory()
    {
        SubscribeLocalEvent<PdaMessagingHistoryComponent, ComponentInit>(OnHistoryInit);
    }

    private void OnHistoryInit(Entity<PdaMessagingHistoryComponent> ent, ref ComponentInit args)
    {
        // already data
        if (ent.Comp.MessageCount > 0)
            return;

        ent.Comp.Messages = new IChatMessage[ent.Comp.MaxHistory];
        ent.Comp.MessageCount = 0;
        Dirty(ent);
    }

    public IEnumerable<IChatMessage> GetFullHistory(Entity<PdaMessagingHistoryComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        for (var i = 0; i < ent.Comp.MessageCount; i++)
            yield return ent.Comp.Messages[i];
    }

    public IEnumerable<IChatMessage> GetHistory(Entity<PdaMessagingHistoryComponent?> ent, int count)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            yield break;

        if (count > ent.Comp.MessageCount)
            count = ent.Comp.MessageCount;

        for (var i = 0; i < count; i++)
            yield return ent.Comp.Messages[i];
    }
}
