using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Item.ItemToggle;

public sealed class ItemToggleOnMobThresholdsSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleOnMobThresholdsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ItemToggleOnMobThresholdsComponent, OrganInitEvent>(OnOrganInit);

        SubscribeLocalEvent<ItemToggleOnMobThresholdsComponent, MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<ItemToggleOnMobThresholdsComponent, BodyOrganRelayedEvent<MobStateChangedEvent>>(OnBodyStateChanged);
    }

    private void OnInit(Entity<ItemToggleOnMobThresholdsComponent> ent, ref ComponentInit args)
    {
        if (HasComp<OrganComponent>(ent.Owner))
            return;

        if (!TryComp<MobStateComponent>(ent.Owner, out var stateComp))
            return;

        CheckItemToggle(ent, stateComp.CurrentState);
    }

    private void OnOrganInit(Entity<ItemToggleOnMobThresholdsComponent> ent, ref OrganInitEvent args)
    {
        if (!TryComp<MobStateComponent>(args.Body, out var stateComp))
            return;

        CheckItemToggle(ent, stateComp.CurrentState);
    }

    private void OnStateChanged(Entity<ItemToggleOnMobThresholdsComponent> ent, ref MobStateChangedEvent args)
    {
        CheckItemToggle(ent, args.NewMobState);
    }

    private void OnBodyStateChanged(Entity<ItemToggleOnMobThresholdsComponent> ent, ref BodyOrganRelayedEvent<MobStateChangedEvent> args)
    {
        CheckItemToggle(ent, args.Args.NewMobState);
    }

    private void CheckItemToggle(Entity<ItemToggleOnMobThresholdsComponent> ent, MobState state)
    {
        if (!ent.Comp.States.Contains(state))
            return;

        switch (ent.Comp.Mode)
        {
            case ItemToggleOnMobThresholdsMode.Toggle:
                _itemToggle.Toggle(ent.Owner);
                break;
            case ItemToggleOnMobThresholdsMode.Enable:
                _itemToggle.TryActivate(ent.Owner);
                break;
            case ItemToggleOnMobThresholdsMode.Disable:
                _itemToggle.TryDeactivate(ent.Owner);
                break;
        }
    }
}
