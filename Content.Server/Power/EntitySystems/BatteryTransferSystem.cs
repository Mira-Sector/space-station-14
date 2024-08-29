using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;

namespace Content.Server.Power.EntitySystems;

public sealed class BatteryTransferSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BatteryTransferComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, BatteryTransferComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BatteryComponent>(uid, out var recieverBattery) || !TryComp<BatteryTransferComponent>(uid, out var revieverTransfer))
            return;

        if (!TryComp<BatteryComponent>(args.Used, out var senderBattery) || !TryComp<BatteryTransferComponent>(args.Used, out var senderTransfer))
            return;

        if (!revieverTransfer.CanRecieve || !senderTransfer.CanTransfer)
            return;

        var targetEnt = Identity.Entity(uid, EntityManager);
        var sourceEnt = Identity.Entity(args.Used, EntityManager);

        if (_battery.IsFull(uid, recieverBattery))
        {
            _popup.PopupEntity(Loc.GetString("battery-transfer-full", ("target", targetEnt)), args.User, args.User);
            args.Handled = true;
            return;
        }

        // checks if it has no power with a 1% margin
        if (senderBattery.CurrentCharge / senderBattery.MaxCharge < 0.01f)
        {
            _popup.PopupEntity(Loc.GetString("battery-transfer-empty", ("source", sourceEnt)), args.User, args.User);
            args.Handled = true;
            return;
        }

        float recieverDelta = recieverBattery.MaxCharge - recieverBattery.CurrentCharge;

        if (senderBattery.CurrentCharge > recieverDelta)
        {
            _battery.SetCharge(uid, recieverBattery.MaxCharge, recieverBattery);
            _battery.SetCharge(args.Used, senderBattery.CurrentCharge - recieverDelta, senderBattery);
        }
        else
        {
            _battery.SetCharge(uid, recieverBattery.CurrentCharge + senderBattery.CurrentCharge, recieverBattery);
            _battery.SetCharge(args.Used, 0f, senderBattery);
        }

        _popup.PopupEntity(Loc.GetString("battery-transfer-power", ("source", sourceEnt), ("target", targetEnt)), args.User, args.User);

        args.Handled = true;
    }
}
