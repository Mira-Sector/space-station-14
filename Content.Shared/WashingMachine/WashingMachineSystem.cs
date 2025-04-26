using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.WashingMachine;

public sealed partial class WashingMachineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string SlotPrefix = "washing-machine-slot-";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WashingMachineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WashingMachineComponent, ComponentRemove>(OnRemoved);

        SubscribeLocalEvent<WashingMachineComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<WashingMachineComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<WashingMachineComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.WashingMachineState != WashingMachineState.Washing)
                continue;

            if (component.WashingFinished > _timing.CurTime)
                continue;

            component.WashingMachineState = WashingMachineState.Idle;
            DirtyField(uid, component, nameof(WashingMachineComponent.WashingMachineState));

            foreach (var (slotId, slot) in component.Slots)
            {
                _slots.TryEject(uid, slot, null, out _);
                _slots.SetLock(uid, slotId, false);
            }
        }
    }

    private void OnInit(Entity<WashingMachineComponent> ent, ref ComponentInit args)
    {
        _appearance.SetData(ent.Owner, WashingMachineVisuals.State, ent.Comp.WashingMachineState);

        for (var i = 0; i < ent.Comp.SlotCount; i++)
        {
            var slot = new ItemSlot();
            var slotId = GetSlotId(i);
            _slots.AddItemSlot(ent.Owner, slotId, slot);
            ent.Comp.Slots.Add(slotId, slot);
        }

        DirtyField(ent.Owner, ent.Comp, nameof(WashingMachineComponent.Slots));
    }

    private void OnRemoved(Entity<WashingMachineComponent> ent, ref ComponentRemove args)
    {
        foreach (var slot in ent.Comp.Slots.Values)
            _slots.RemoveItemSlot(ent.Owner, slot);

        ent.Comp.Slots.Clear();
        DirtyField(ent.Owner, ent.Comp, nameof(WashingMachineComponent.Slots));
    }

    private static string GetSlotId(int i)
    {
        return SlotPrefix + i;
    }

    private void OnBreak(Entity<WashingMachineComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.WashingMachineState = WashingMachineState.Broken;
        DirtyField(ent.Owner, ent.Comp, nameof(WashingMachineComponent.WashingMachineState));

        foreach (var slot in ent.Comp.Slots.Values)
        {
            _slots.TryEject(ent.Owner, slot, null, out _);
            _slots.SetLock(ent.Owner, slot, true);
        }
    }

    private void OnActivateInWorld(Entity<WashingMachineComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!CanActivate(ent))
            return;

        args.Handled = true;
        Activate(ent);
    }

    private void OnGetVerbs(Entity<WashingMachineComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract || !args.CanComplexInteract)
            return;

        if (!CanActivate(ent))
            return;

        var verb = new ActivationVerb()
        {
            Text = Loc.GetString("washing-machine-start"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Act = () => Activate(ent)
        };

        args.Verbs.Add(verb);
    }

    private bool CanActivate(Entity<WashingMachineComponent> ent)
    {
        if (ent.Comp.WashingMachineState != WashingMachineState.Idle)
            return false;

        if (!_power.IsPowered(ent.Owner))
            return false;

        return true;
    }

    private void Activate(Entity<WashingMachineComponent> ent)
    {
        ent.Comp.WashingFinished = _timing.CurTime + ent.Comp.WashingTime;
        DirtyField(ent.Owner, ent.Comp, nameof(WashingMachineComponent.WashingFinished));

        ent.Comp.WashingMachineState = WashingMachineState.Washing;
        DirtyField(ent.Owner, ent.Comp, nameof(WashingMachineComponent.WashingMachineState));

        foreach (var slot in ent.Comp.Slots.Values)
            _slots.SetLock(ent.Owner, slot, true);
    }
}
