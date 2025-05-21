using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Silicons.Sync.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SiliconSyncableMonitoringConsoleComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2f);

    [ViewVariables]
    public HashSet<EntityUid> Users = [];
}
