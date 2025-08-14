using Content.Shared.Body.Part;
using Content.Shared.Inventory;

namespace Content.Shared.Surgery.Events;

[ByRefEvent]
public sealed partial class SurgeryInteractionAttemptEvent(EntityUid? body, EntityUid? limb, EntityUid? used, EntityUid user, BodyPart? part) : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public readonly EntityUid? Body = body;
    public readonly EntityUid? Limb = limb;

    public readonly EntityUid? Used = used;
    public readonly EntityUid User = user;

    public readonly BodyPart? Part = part;

    public string? Reason;
}
