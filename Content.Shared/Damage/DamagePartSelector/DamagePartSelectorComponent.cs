using Content.Shared.Actions;
using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.DamageSelector;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamagePartSelectorComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public BodyPart SelectedPart = new(BodyPartType.Torso, BodyPartSymmetry.None);

    [DataField(required: true)]
    public List<DamagePartSelectorEntry> SelectableParts = [];

    [ViewVariables]
    public EntityUid? Action;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DamagePartSelectorEntry
{
    [DataField]
    public BodyPart BodyPart;

    [DataField]
    public SpriteSpecifier Sprite;
}

public sealed partial class DamageSelectorActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public enum DamageSelectorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DamageSelectorSystemMessage : BoundUserInterfaceMessage
{
    public BodyPart Part;

    public DamageSelectorSystemMessage(BodyPart part)
    {
        Part = part;
    }
}
