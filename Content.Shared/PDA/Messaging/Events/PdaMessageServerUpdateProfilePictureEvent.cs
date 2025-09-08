using Robust.Shared.Prototypes;

namespace Content.Shared.PDA.Messaging.Events;

[ByRefEvent]
public readonly record struct PdaMessageServerUpdateProfilePictureEvent(EntityUid Client, ProtoId<PdaChatProfilePicturePrototype> ProfilePicture);
