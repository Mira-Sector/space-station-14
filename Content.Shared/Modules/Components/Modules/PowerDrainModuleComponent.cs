using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Modules.Components.Modules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerDrainModuleComponent : BaseToggleableModuleComponent
{
    [DataField, AutoNetworkedField]
    public PowerDrainEntry? EnabledDraw;

    [DataField, AutoNetworkedField]
    public PowerDrainEntry? DisabledDraw;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class PowerDrainEntry
{
    [DataField]
    public float Additional;

    [DataField]
    public float Multiplier;
}
