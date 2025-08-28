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

    public abstract SpriteSpecifier GetUiIcon(IPrototypeManager prototype);
    public abstract string GetUiName();
    public abstract string GetNotificationText();

    public override bool Equals(object? obj)
    {
        return obj is BasePdaChatMessageable other && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
