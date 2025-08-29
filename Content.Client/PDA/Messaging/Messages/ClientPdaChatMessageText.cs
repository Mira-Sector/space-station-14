using Content.Client.PDA.Messaging.Messages.Controls;
using Content.Shared.PDA.Messaging.Messages;
using Robust.Client.UserInterface;

namespace Content.Client.PDA.Messaging.Messages;

public sealed partial class ClientPdaChatMessageText(PdaChatMessageText shared) : IClientPdaChatMessage
{
    private readonly PdaChatMessageText _shared = shared;

    public Control GetUiControl() => new PdaChatMessageTextControl(_shared.Contents);
}
