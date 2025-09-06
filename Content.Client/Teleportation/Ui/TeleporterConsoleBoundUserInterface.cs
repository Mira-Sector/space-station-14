using System.Numerics;
using System.Collections.Immutable;
using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using Robust.Client.UserInterface;
using Content.Shared.Destructible;
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

        if (teleComp.LinkedTeleporter != null)
        {
            var (uid, meta) = EntMan.GetEntityData(teleComp.LinkedTeleporter ?? NetEntity.Invalid);
            _menu.LinkedTeleporter = meta.EntityName;
        }

        int coordX = 0;
        int coordY = 0;
        bool coordXValid = false;
        bool coordYValid = false;
        bool beaconValid = false;
        TeleportPoint selectedBeacon = new TeleportPoint();
        var logMan = IoCManager.Resolve<ILogManager>();
        var log = logMan.RootSawmill;
        log.Debug($"UI");
        _menu.UpdateTeleportButtons(false);
        _menu.Beacons = teleComp.BeaconList;
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

}



