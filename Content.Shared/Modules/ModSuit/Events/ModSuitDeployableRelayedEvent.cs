namespace Content.Shared.Modules.ModSuit.Events;

public sealed partial class ModSuitDeployableRelayedEvent<T>(T args, EntityUid modSuit, int partNumber) : EntityEventArgs
{
    public T Args = args;
    public readonly EntityUid ModSuit = modSuit;
    public readonly int PartNumber = partNumber;
}
