using System.Numerics;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Robust.Client.UserInterface;

namespace Content.Client.Teleportation.Ui;

public sealed class TeleporterConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TeleporterConsoleUI? _menu;

    public TeleporterConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleporterConsoleUI>();

        if (!EntMan.TryGetComponent<TeleporterConsoleComponent>(Owner, out var teleComp))
            return;

        if (teleComp.LinkedTeleporter != null) //set link name
        {
            var (uid, meta) = EntMan.GetEntityData(teleComp.LinkedTeleporter ?? NetEntity.Invalid);
            _menu.SetLinkName(Loc.GetString("teleporter-linked-to", ("name", meta.EntityName))); //kind of want a sprite here as well
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
                SendPredictedMessage(new TeleporterActivateMessage(new Vector2(coordX, coordY), send));
            else
            {
                if (beaconValid == true)
                    SendPredictedMessage(new TeleporterActivateBeaconMessage(selectedBeacon, send));
            }
        };

        _menu.ReceiveClicked += (send) =>
        {
            if (coordXValid == true && coordYValid == true) //require values to be input before teleporter can be sent
                SendPredictedMessage(new TeleporterActivateMessage(new Vector2(coordX, coordY), send));
            else
            {
                if (beaconValid == true)
                    SendPredictedMessage(new TeleporterActivateBeaconMessage(selectedBeacon, send));
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
            if (!EntMan.TryGetComponent<TeleporterBeaconComponent>(beaconEnt, out var beaconComp)) //if it does, does the component exist?
                continue;
            if (beaconComp.ValidBeacon) //if it does, is it a valid beacon?
                validList.Add(beacon);
        }
        return validList;
    }

}



