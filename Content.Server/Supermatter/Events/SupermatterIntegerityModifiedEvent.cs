using Content.Shared.FixedPoint;

namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterIntegerityModifiedEvent : EntityEventArgs
{
    public FixedPoint2 CurrentIntegerity;
    public FixedPoint2 PreviousIntegerity;

    public SupermatterIntegerityModifiedEvent(FixedPoint2 currentIntegerity, FixedPoint2 previousIntegerity)
    {
        CurrentIntegerity = currentIntegerity;
        PreviousIntegerity = previousIntegerity;
    }
}
