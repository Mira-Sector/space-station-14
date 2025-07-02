using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.DamageSelector;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamagePartSelectorComponent : Component
{
    [ViewVariables, AutoNetworkedField, Access(typeof(DamagePartSelectorSystem))]
    public BodyPart SelectedPart = new(BodyPartType.Torso, BodyPartSymmetry.None);

    [DataField(required: true)]
    public DamagePartSelectorEntry[] SelectableParts;

    [DataField]
    public BodyPart MainPart = new(BodyPartType.Torso, BodyPartSymmetry.None);
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DamagePartSelectorEntry
{
    [DataField(required: true)]
    public BodyPart BodyPart;

    [DataField(required: true)]
    public SpriteSpecifier Sprite;
}

[Serializable, NetSerializable]
public enum DamageSelectorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DamageSelectorSystemMessage(BodyPart part) : BoundUserInterfaceMessage
{
    public readonly BodyPart Part = part;
}
