using Content.Shared.PDA.Messaging.Messages;

namespace Content.Client.PDA.Messaging.Messages;

public interface IClientPdaChatMessageFactory
{
    IClientPdaChatMessage Create(BasePdaChatMessage message);
}

public sealed class ClientPdaChatMessageFactory : IClientPdaChatMessageFactory
{
    public IClientPdaChatMessage Create(BasePdaChatMessage message)
    {
        return message switch
        {
            PdaChatMessageText text => new ClientPdaChatMessageText(text),
            _ => throw new NotImplementedException(),
        };
    }
}
