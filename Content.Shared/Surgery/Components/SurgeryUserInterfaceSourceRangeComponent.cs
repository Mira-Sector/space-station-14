using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SurgeryUserInterfaceSourceRangeComponent : Component
{
    [DataField]
    public float Range = 0.5f;

    [DataField]
    public TimeSpan RangeCheckUpdateDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RangeCheckNextUpdate;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? LastInRange;

    public const LookupFlags Flags = LookupFlags.Dynamic | LookupFlags.Sundries;
}
