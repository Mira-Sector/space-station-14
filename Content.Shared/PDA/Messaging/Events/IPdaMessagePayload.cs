namespace Content.Shared.PDA.Messaging.Events;

public interface IPdaMessagePayload
{
    NetEntity Client { get; }

    void RunAction(IEntityManager entity);
}
