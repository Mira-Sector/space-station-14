namespace Content.Shared.PDA;

[ByRefEvent]
public readonly record struct PdaOwnerChangedEvent(EntityUid? Owner, string? OwnerName);
