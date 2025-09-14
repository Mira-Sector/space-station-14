namespace Content.Shared.PDA.Messaging.Events;

public interface IPdaMessagePayload
{
    NetEntity Client { get; }

    void RunAction(IEntityManager entity)
    {
        var client = entity.GetEntity(Client);
        entity.EventBus.RaiseLocalEvent(client, this);
    }
}
