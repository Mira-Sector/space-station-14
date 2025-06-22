using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class ItemToggleOnBodyDamageSystem : BaseOnBodyDamageSystem<ItemToggleOnBodyDamageComponent>
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleOnBodyDamageComponent, BodyDamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<ItemToggleOnBodyDamageComponent> ent, ref BodyDamageChangedEvent args)
    {
        if (!CanDoEffect(ent))
            return;

        _itemToggle.TrySetActive(ent.Owner, ent.Comp.EnableOnTrigger);
    }
}
