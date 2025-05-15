using Content.Shared.FixedPoint;

namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterIntegerityModifiedEvent : EntityEventArgs
{
    public FixedPoint2 CurrentIntegerity;
    public FixedPoint2 PreviousIntegerity;
    public FixedPoint2 MaxIntegerity;

    public SupermatterIntegerityModifiedEvent(FixedPoint2 currentIntegerity, FixedPoint2 previousIntegerity, FixedPoint2 maxIntegerity)
    {
        CurrentIntegerity = currentIntegerity;
        PreviousIntegerity = previousIntegerity;
        MaxIntegerity = maxIntegerity;
    }
}
