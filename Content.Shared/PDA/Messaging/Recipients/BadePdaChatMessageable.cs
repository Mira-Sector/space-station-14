using Content.Shared.PDA.Messaging.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.PDA.Messaging.Recipients;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class BasePdaChatMessageable
{
    public const int SegmentMax = 999;
    public const int IntegersPerSegment = 3;

    [DataField]
    public string Id = default!;

    public abstract string Prefix();

    public abstract IEnumerable<PdaChatRecipientProfile> GetRecipients();
    public abstract BasePdaChatMessageable GetRecipientMessageable(BasePdaChatMessage message);

    public abstract SpriteSpecifier GetUiIcon(IPrototypeManager prototype);
    public abstract string GetUiName();
    public abstract string GetNotificationText();

    public override bool Equals(object? obj)
    {
        return obj is BasePdaChatMessageable other && Id == other.Id;
    }

    public static bool operator ==(BasePdaChatMessageable? a, BasePdaChatMessageable? b)
    {
        if (a is not null)
            return a.Equals(b);
        else
            return b is null;
    }

    public static bool operator !=(BasePdaChatMessageable? a, BasePdaChatMessageable? b)
    {
        if (a is not null)
            return !a.Equals(b);
        else
            return b is not null;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public BasePdaChatMessageable(BasePdaChatMessageable messageable)
    {
        Id = messageable.Id;
    }
}
