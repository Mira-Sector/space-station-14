using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging.Messages;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BasePdaChatMessage
{
    [DataField]
    public PdaChatRecipientProfile Sender;

    [DataField]
    public IPdaChatRecipient Recipient;

    public abstract string GetNotificationText();
}
