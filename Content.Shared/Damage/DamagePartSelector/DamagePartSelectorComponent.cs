using Content.Shared.Actions;
using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.DamageSelector;

[RegisterComponent, NetworkedComponent]
public sealed partial class DamagePartSelectorComponent : Component
{
    [ViewVariables]
    public BodyPart SelectedPart = new BodyPart(BodyPartType.Torso, BodyPartSymmetry.None);

    [DataField(required: true)]
    public Dictionary<BodyPart, SpriteSpecifier> SelectableParts = new();

    [ViewVariables]
    public EntityUid? Action;
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
