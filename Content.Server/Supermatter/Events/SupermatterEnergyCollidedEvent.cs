namespace Content.Server.Supermatter.Events;

public sealed partial class SupermatterEnergyCollidedEvent : EntityEventArgs
{
    public EntityUid Supermatter;
    public float Energy;

    public SupermatterEnergyCollidedEvent(EntityUid supermatter, float energy)
    {
        Supermatter = supermatter;
        Energy = energy;
    }
}
