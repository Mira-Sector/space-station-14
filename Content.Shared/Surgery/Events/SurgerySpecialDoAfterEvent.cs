using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Surgery.Events;

[Serializable, NetSerializable]
public sealed partial class SurgerySpecialDoAfterEvent : DoAfterEvent
{
    public SurgerySpecial Special { get; private set; }

    public BodyPart? BodyPart { get; private set; }

    public SurgerySpecialDoAfterEvent(SurgerySpecial special, BodyPart? bodyPart)
    {
        Special = special;
        BodyPart = bodyPart;
    }

    public override DoAfterEvent Clone() => this;
}
