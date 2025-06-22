using Content.Shared.Body.Damage.Components;
using Content.Shared.Body.Damage.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.Body.Damage.Systems;

public sealed partial class ItemToggleOnBodyDamageSystem : BaseOnBodyDamageSystem<ItemToggleOnBodyDamageComponent>
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleOnBodyDamageComponent, BodyDamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<ItemToggleOnBodyDamageComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<ItemToggleOnBodyDamageComponent, ItemToggleDeactivateAttemptEvent>(OnDeactivateAttempt);
    }

    private void OnDamageChanged(Entity<ItemToggleOnBodyDamageComponent> ent, ref BodyDamageChangedEvent args)
    {
        if (!CanDoEffect(ent))
        {
            ent.Comp.Triggered = false;
            Dirty(ent);
            return;
        }

        _itemToggle.TrySetActive(ent.Owner, ent.Comp.EnableOnTrigger);
        ent.Comp.Triggered = true;
        Dirty(ent);
    }

    private void OnActivateAttempt(Entity<ItemToggleOnBodyDamageComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.PreventToggle)
            return;

        if (!ent.Comp.Triggered)
            return;

        args.Cancelled = true;

        if (ent.Comp.PreventToggleReason != null)
            args.Popup = Loc.GetString(ent.Comp.PreventToggleReason.Value);
    }

    private void OnDeactivateAttempt(Entity<ItemToggleOnBodyDamageComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.PreventToggle)
            return;

        if (!ent.Comp.Triggered)
            return;

        args.Cancelled = true;

        if (ent.Comp.PreventToggleReason != null)
            args.Popup = Loc.GetString(ent.Comp.PreventToggleReason.Value);
    }
}
