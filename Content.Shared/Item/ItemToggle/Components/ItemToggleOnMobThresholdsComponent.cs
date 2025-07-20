using Content.Shared.Mobs;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Item.ItemToggle.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleOnMobThresholdsComponent : Component
{
    [DataField]
    public ItemToggleOnMobThresholdsMode Mode;

    [DataField]
    public HashSet<MobState> States = [];
}

[Serializable, NetSerializable]
public enum ItemToggleOnMobThresholdsMode : byte
{
    Toggle,
    Enable,
    Disable
}
