using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;

namespace Content.Shared.Body.Events;

[ByRefEvent]
public readonly record struct BodyChangedEvent(Entity<BodyComponent> Body);

[ByRefEvent]
public readonly record struct BodyInitEvent(Entity<BodyComponent> Body);

[ByRefEvent]
public readonly record struct LimbInitEvent(Entity<BodyPartComponent> Part, Entity<BodyComponent> Body);

[ByRefEvent]
public readonly record struct OrganInitEvent(Entity<OrganComponent> Organ, Entity<BodyPartComponent> Part, Entity<BodyComponent> Body);
