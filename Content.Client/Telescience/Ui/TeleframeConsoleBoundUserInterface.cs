using System.Numerics;
using Content.Shared.Teleportation.Components;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience;
using Robust.Client.UserInterface;
using Content.Shared.Atmos.Components;

namespace Content.Client.Telescience.Ui;

public sealed class TeleframeConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TeleframeConsoleUI? _menu;

    public TeleframeConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleframeConsoleUI>();

        if (!EntMan.TryGetComponent<TeleframeConsoleComponent>(Owner, out var teleComp))
            return;

        if (teleComp.LinkedTeleframe != null) //set link name
        {
            var (uid, meta) = EntMan.GetEntityData(teleComp.LinkedTeleframe ?? NetEntity.Invalid);
            _menu.SetLinkName(Loc.GetString("teleporter-linked-to", ("name", meta.EntityName), ("state", GetChargeState(uid)))); //kind of want a sprite here as well
        }
        else
        {
            _menu.SetLinkName(Loc.GetString("teleporter-linked-to", ("name", Loc.GetString("teleporter-linked-default"))));
        }

        var coordX = 0;
        var coordY = 0;
        var coordXValid = false;
        var coordYValid = false;
        var beaconValid = false;
        TeleportPoint selectedBeacon = new TeleportPoint();

        _menu.UpdateTeleportButtons(false);
        _menu.Beacons = GetValidBeacons(teleComp.BeaconList);
        _menu.AddBeaconButtons();

        _menu.OnCoordsXChanged += (coord) =>
        {
            beaconValid = false; //if typing in text, invalidate beacon teleport
            if (teleComp.MaxRange == null || Math.Abs(coord) < teleComp.MaxRange) //limit maximum value, currently absolute coordinate value rather than actual range.
            {
                coordX = coord;
                coordXValid = true; //if integer in range, valid
            }
            else
            {
                _menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-bigrange", ("range", teleComp.MaxRange.ToString()!)));
                coordYValid = false; //not in range, invalid
            }

            if (coordXValid && coordYValid)
                _menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-custom", ("X", coordX), ("Y", coordY))); //both are valid, so indicate ready to teleport

            _menu.UpdateTeleportButtons(coordXValid && coordYValid);
        };

        _menu.OnCoordsYChanged += (coord) =>
        {
            beaconValid = false; //if typing in text, invalidate beacon teleport
            if (teleComp.MaxRange == null || Math.Abs(coord) < teleComp.MaxRange) //limit maximum value, currently absolute coordinate value rather than actual range.
            {
                coordY = coord;
                coordYValid = true; //if integer in range, valid
            }
            else
            {
                _menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-bigrange", ("range", teleComp.MaxRange.ToString()!)));
                coordYValid = false;  //not in range, invalid
            }

            if (coordXValid && coordYValid)
                _menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-custom", ("X", coordX), ("Y", coordY))); //both are valid, so indicate ready to teleport

            _menu.UpdateTeleportButtons(coordXValid && coordYValid);
        };

        _menu.SendClicked += (send) =>
        { //for beacons have an if that is true if beacon selected and false if not. If true, use a seperate activate message.
            if (coordXValid == true && coordYValid == true) //require values to be input before teleport can be sent
                SendPredictedMessage(new TeleframeActivateMessage(new Vector2(coordX, coordY), send));
            else
            {
                if (beaconValid == true)
                    SendPredictedMessage(new TeleframeActivateBeaconMessage(selectedBeacon, send));
            }
        };

        _menu.ReceiveClicked += (send) =>
        {
            if (coordXValid == true && coordYValid == true) //require values to be input before Teleframe can be sent
                SendPredictedMessage(new TeleframeActivateMessage(new Vector2(coordX, coordY), send));
            else
            {
                if (beaconValid == true)
                    SendPredictedMessage(new TeleframeActivateBeaconMessage(selectedBeacon, send));
            }
        };

        _menu.BeaconClicked += (beacon) =>
        {
            _menu.SetCoordsX(int.Parse("")); _menu.SetCoordsY(int.Parse("")); //if clicking a beacon, invalidate coordinate teleport
            coordXValid = false; coordYValid = false;
            _menu.UpdateTeleportButtons(true);
            _menu.UpdateTeleportSummary(Loc.GetString("teleporter-summary-beacon", ("beacon", beacon.Location)));
            beaconValid = true;
            selectedBeacon = beacon;
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

    public string GetChargeState(EntityUid uid)
    {
        if (EntMan.TryGetComponent<TeleframeChargingComponent>(uid, out var charge))
        {
            var timeLeft = (charge.EndTime - charge.Duration).TotalSeconds;
            return Loc.GetString("teleporter-charging", ("time", timeLeft));
        }

        if (EntMan.TryGetComponent<TeleframeRechargingComponent>(uid, out var recharge))
        {
            var timeLeft = (recharge.EndTime - recharge.Duration).TotalSeconds;
            return Loc.GetString("teleporter-charging", ("time", timeLeft));
        }

        return Loc.GetString("teleporter-active");
    }

}



