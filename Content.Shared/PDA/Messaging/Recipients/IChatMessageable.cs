using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Recipients;

public interface IChatRecipient
{
    SpriteSpecifier GetUiIcon(IPrototypeManager prototype);
    string GetUiName();
    string GetNotificationText();
}
