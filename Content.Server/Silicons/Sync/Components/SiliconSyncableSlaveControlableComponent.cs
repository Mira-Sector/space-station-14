using Robust.Shared.Map;

namespace Content.Server.Silicons.Sync.Components;

[RegisterComponent, Access(typeof(SiliconSyncSystem))]
public sealed partial class SiliconSyncableSlaveControllableComponent : Component
{
    [ViewVariables]
    public EntityCoordinates? TargetCoordinates;
}
