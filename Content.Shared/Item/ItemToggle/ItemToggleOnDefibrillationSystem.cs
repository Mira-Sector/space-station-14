using Content.Shared.Body.Part;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Medical;

namespace Content.Shared.Item.ItemToggle;

public sealed class ItemToggleOnDefibrillationSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleOnDefibrillationComponent, TargetDefibrillatedEvent>(OnDefib);
        SubscribeLocalEvent<ItemToggleOnDefibrillationComponent, BodyOrganRelayedEvent<TargetDefibrillatedEvent>>(OnOrganDefib);
    }

    private void OnDefib(Entity<ItemToggleOnDefibrillationComponent> ent, ref TargetDefibrillatedEvent args)
    {
        _itemToggle.TrySetActive(ent.Owner, ent.Comp.Enable);
    }

    private void OnOrganDefib(Entity<ItemToggleOnDefibrillationComponent> ent, ref BodyOrganRelayedEvent<TargetDefibrillatedEvent> args)
    {
        _itemToggle.TrySetActive(ent.Owner, ent.Comp.Enable);
    }
}
