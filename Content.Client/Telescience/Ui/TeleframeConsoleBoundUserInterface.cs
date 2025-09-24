using System.Numerics;
using Content.Shared.Teleportation.Components;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;
using Robust.Client.GameObjects;

namespace Content.Client.Telescience.Ui;

public sealed class TeleframeConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TeleframeConsoleUI? _menu;
    private readonly IEntityManager _entMan;
    private readonly SharedTransformSystem _transform;
    [Dependency] private readonly IGameTiming _timing = default!;

    public TeleframeConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _entMan = IoCManager.Resolve<IEntityManager>();
        _transform = _entMan.System<TransformSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleframeConsoleUI>();

        if (!EntMan.TryGetComponent<TeleframeConsoleComponent>(Owner, out var teleComp))
            return;

        if (!EntMan.TryGetComponent<TransformComponent>(Owner, out var transComp))
            return;

        var pos = _transform.GetMapCoordinates(Owner, xform: transComp); //get map coordinates for max range and setting map ID for custom coordinates
        var coordX = 0;
        var coordY = 0;
        var coordXValid = false;
        var coordYValid = false;
        var beaconValid = false;
        TeleportPoint selectedBeacon = new TeleportPoint();


        _menu.Beacons = GetValidBeacons(teleComp.BeaconList);
        _menu.AddBeaconButtons();
        TeleportCheck(_menu, false, Loc.GetString("teleporter-summary-insufficient"));

        _menu.OnCoordsXChanged += (coord) =>
        {
            beaconValid = false; //if typing in text, invalidate beacon teleport
            var message = Loc.GetString("teleporter-summary-insufficient");
            if (teleComp.MaxRange == null || Math.Abs(coord) < Math.Abs(pos.X + (float)teleComp.MaxRange)) //range from console rather than teleporter because it's simpler to code.
            {
                coordX = coord;
                coordXValid = true; //if integer in range, valid
            }
            else
            {
                message = Loc.GetString("teleporter-summary-bigrange", ("range", teleComp.MaxRange.ToString()!));
                coordYValid = false; //not in range, invalid
            }

            if (coordXValid && coordYValid)
                message = Loc.GetString("teleporter-summary-custom", ("X", coordX), ("Y", coordY)); //both are valid, so indicate ready to teleport

            TeleportCheck(_menu, coordXValid && coordYValid, message);
        };

        _menu.OnCoordsYChanged += (coord) =>
        {
            beaconValid = false; //if typing in text, invalidate beacon teleport
            var message = Loc.GetString("teleporter-summary-insufficient");
            if (teleComp.MaxRange == null || Math.Abs(coord) < Math.Abs(pos.Y + (float)teleComp.MaxRange)) //range from console rather than teleporter because it's simpler to code.
            {
                coordY = coord;
                coordYValid = true; //if integer in range, valid
            }
            else
            {
                message = Loc.GetString("teleporter-summary-bigrange", ("range", teleComp.MaxRange.ToString()!));
                coordYValid = false;  //not in range, invalid
            }

            if (coordXValid && coordYValid)
                message = Loc.GetString("teleporter-summary-custom", ("X", coordX), ("Y", coordY)); //both are valid, so indicate ready to teleport

            TeleportCheck(_menu, coordXValid && coordYValid, message);
        };

        _menu.SendClicked += (send) =>
        { //for beacons have an if that is true if beacon selected and false if not. If true, use a seperate activate message.
            if (coordXValid == true && coordYValid == true) //require values to be input before teleport can be sent
            {
                SendPredictedMessage(new TeleframeActivateMessage(new Vector2(coordX, coordY), send));
                TeleportCheck(_menu, false, Loc.GetString("teleporter-summary-notready"));
            }
            else
            {
                if (beaconValid == true)
                {
                    SendPredictedMessage(new TeleframeActivateBeaconMessage(selectedBeacon, send));
                    TeleportCheck(_menu, false, Loc.GetString("teleporter-summary-notready"));
                }
            }
        };

        _menu.ReceiveClicked += (send) =>
        {
            if (coordXValid == true && coordYValid == true) //require values to be input before Teleframe can be sent
            {
                SendPredictedMessage(new TeleframeActivateMessage(new Vector2(coordX, coordY), send));
                TeleportCheck(_menu, false, Loc.GetString("teleporter-summary-notready"));
            }
            else
            {
                if (beaconValid == true)
                {
                    SendPredictedMessage(new TeleframeActivateBeaconMessage(selectedBeacon, send));
                    TeleportCheck(_menu, false, Loc.GetString("teleporter-summary-notready"));
                }
            }
        };

        _menu.BeaconClicked += (beacon) =>
        {
            _menu.SetCoordsX(0); _menu.SetCoordsY(0); //if clicking a beacon, invalidate coordinate teleport
            coordXValid = false; coordYValid = false;
            beaconValid = true;
            selectedBeacon = beacon;
            TeleportCheck(_menu, true, Loc.GetString("teleporter-summary-beacon", ("beacon", beacon.Location)));
        };

        _menu.RefreshClicked += (valid, summary) =>
        {
            TeleportCheck(_menu, !valid, summary);
        };
    }

    public HashSet<TeleportPoint> GetValidBeacons(HashSet<TeleportPoint> totalList) //get valid beacons only, also make sure beacons exist!
    {
        HashSet<TeleportPoint> validList = new();
        foreach (var beacon in totalList)
        {
            if (!EntMan.TryGetEntity(beacon.TelePoint, out var beaconEnt)) //does the entity exist?
                continue;
            if (!EntMan.TryGetComponent<TeleframeBeaconComponent>(beaconEnt, out var beaconComp)) //if it does, does the component exist?
                continue;
            if (beaconComp.ValidBeacon) //if it does, is it a valid beacon?
                validList.Add(beacon);
        }
        return validList;
    }

    public string GetChargeState(EntityUid uid, TeleframeComponent tpComp)
    {
        if (tpComp.IsPowered == false)
            return Loc.GetString("teleporter-unpowered");

        if (EntMan.TryGetComponent<TeleframeChargingComponent>(uid, out var charge))
        {
            var timeLeft = (int)(charge.EndTime - _timing.CurTime).TotalSeconds;
            return Loc.GetString("teleporter-charging", ("time", timeLeft));
        }

        if (EntMan.TryGetComponent<TeleframeRechargingComponent>(uid, out var recharge))
        {
            var timeLeft = (int)(recharge.EndTime - _timing.CurTime).TotalSeconds;
            return Loc.GetString("teleporter-recharging", ("time", timeLeft));
        }

        return Loc.GetString("teleporter-active");
    }
    //check if teleportation console and linked teleframe are valid
    //return true if they are (doesn't mean teleportation is possible)
    //check should be performed any time teleportation possibility changes
    //check should be performed consistently outside this too, not sure how to do that, could just add a refresh button.
    public bool TeleportCheck(TeleframeConsoleUI menu, bool buttons, string message)
    {
        if (!EntMan.TryGetComponent<TeleframeConsoleComponent>(Owner, out var teleComp))
            return false;

        if (teleComp.LinkedTeleframe != null) //set link name
        {
            var (uid, meta) = EntMan.GetEntityData(teleComp.LinkedTeleframe ?? NetEntity.Invalid);
            if (!EntMan.TryGetComponent<TeleframeComponent>(uid, out var tpComp))
                return false;

            menu.SetLinkName(Loc.GetString("teleporter-linked-to", ("name", meta.EntityName), ("state", GetChargeState(uid, tpComp)))); //kind of want a sprite here as well

            if (tpComp.IsPowered == false || tpComp.ReadyToTeleport == false)
            {
                menu.UpdateTeleportButtons(false);
                menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-notready"));
                return true;
            }
        }
        else
        {
            menu.SetLinkName(Loc.GetString("teleporter-linked-to", ("name", Loc.GetString("teleporter-linked-default")), ("state", Loc.GetString("teleporter-linked-default"))));
            menu.UpdateTeleportButtons(false);
            menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-unavailable"));
            return true;
        }

        menu.UpdateTeleportButtons(buttons);
        menu.UpdateTeleportSummary(message);
        return true;
    }

}



