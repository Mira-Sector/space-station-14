using System.Numerics;
using Content.Shared.Ninja.Systems;
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
    public readonly string SumInsufficient = Loc.GetString("teleporter-summary-insufficient");
    public readonly string SumBigRange = Loc.GetString("teleporter-summary-bigrange");
    public readonly string SumReady = Loc.GetString("teleporter-summary-custom");
    public readonly string SumBeacon = Loc.GetString("teleporter-summary-beacon");

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleporterConsoleUI>();

        if (!EntMan.TryGetComponent<TeleporterConsoleComponent>(Owner, out var teleComp))
            return;

        if (teleComp.LinkedTeleporter != null) //set link name
        {
            var (uid, meta) = EntMan.GetEntityData(teleComp.LinkedTeleporter ?? NetEntity.Invalid);
            _menu.SetLinkName(Loc.GetString("teleporter-linked-to") + " " + meta.EntityName);
        }
        else
        {
            _menu.SetLinkName(Loc.GetString("teleporter-linked-to") + " " + Loc.GetString("teleporter-linked-default"));
        }

        int coordX = 0;
        int coordY = 0;
        bool coordXValid = false;
        bool coordYValid = false;
        bool beaconValid = false;
        TeleportPoint selectedBeacon = new TeleportPoint();

        _menu.UpdateTeleportButtons(false);
        _menu.Beacons = GetValidBeacons(teleComp.BeaconList);
        _menu.AddBeaconButtons();

        _menu.OnCoordsXChanged += (text) =>
        {
            beaconValid = false;
            if (int.TryParse(text, out int coord)) //check if valid integer, if not, purge!
            {
                if (Math.Abs(coord) < teleComp.MaxRange) //limit maximum value, currently absolute coordinate value rather than actual range.
                {
                    coordX = coord;
                    coordXValid = true;
                }
                else
                {
                    _menu.SetCoordsX("");
                    _menu.UpdateTeleportSummary(SumBigRange + " " + teleComp.MaxRange.ToString());
                    coordYValid = false;

                }
            }
            else
            {
                _menu.SetCoordsX("");
                _menu.UpdateTeleportSummary(SumInsufficient);
                coordYValid = false;
            }

            if (coordXValid && coordYValid)
                _menu.UpdateTeleportSummary(SumReady + CoordString(coordX, coordY)); //both are valid, so indicate ready to teleport

            _menu.UpdateTeleportButtons(coordXValid && coordYValid);
        };

        _menu.OnCoordsYChanged += (text) =>
        {
            beaconValid = false;
            if (int.TryParse(text, out int coord)) //check if valid integer, if not, purge!
            {
                if (Math.Abs(coord) < teleComp.MaxRange) //limit maximum value, currently absolute coordinate value rather than actual range.
                {
                    coordY = coord;
                    coordYValid = true;
                }
                else
                {
                    _menu.SetCoordsY("");
                    _menu.UpdateTeleportSummary(SumBigRange + " " + teleComp.MaxRange.ToString());
                    coordYValid = false;
                }
            }
            else
            {
                _menu.SetCoordsY("");
                _menu.UpdateTeleportSummary(SumInsufficient);
                coordYValid = false;
            }

            if (coordXValid && coordYValid)
                _menu.UpdateTeleportSummary(SumReady + CoordString(coordX, coordY)); //both are valid, so indicate ready to teleport

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
            _menu.SetCoordsX(""); _menu.SetCoordsY("");
            coordXValid = false; coordYValid = false;
            _menu.UpdateTeleportButtons(true);
            _menu.UpdateTeleportSummary(SumBeacon + " " + beacon.Location);
            beaconValid = true;
            selectedBeacon = beacon;
        };

    }
    public string CoordString(int x, int y)
    {
        return " (" + x.ToString() + ", " + y.ToString() + ")";
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



