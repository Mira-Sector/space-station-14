using Content.Shared.PDA.Messaging.Recipients;

namespace Content.Shared.PDA.Messaging.Messages;

public interface IChatMessage
{
    IChatRecipient Sender { get; set; }
    IChatRecipient Recipient { get; set; }

    string GetNotificationText();
}
