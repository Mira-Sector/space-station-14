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

    public readonly string ErrInsufficient = Loc.GetString("teleporter-summary-insufficient");
    public readonly string ErrReady = Loc.GetString("teleporter-summary-custom");
    public readonly string ErrBigRange = Loc.GetString("teleporter-summary-bigrange");
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

        //_menu.BeaconList = teleComp.BeaconList;
        //_menu.AddBeaconButtons();
        _menu.OnCoordsXChanged += (text) =>
        {
            if (int.TryParse(text, out int coord)) //check if valid integer, if not, purge!
            {
                if (Math.Abs(coord) < teleComp.MaxRange) //limit maximum value, currently absolute coordinate value rather than actual range.
                {
                    coordX = coord;
                    coordXValid = true;
                    if (coordYValid)
                        _menu.UpdateTeleportSummary(ErrReady + CoordString(coordX, coordY)); //both are valid, so indicate ready to teleport
                }
                else
                {
                    _menu.SetCoordsX("");
                    coordYValid = false;
                    _menu.UpdateTeleportSummary(ErrBigRange + " " + teleComp.MaxRange.ToString());
                }
            }
            else
            {
                _menu.SetCoordsX("");
                _menu.UpdateTeleportSummary(ErrInsufficient);
                coordYValid = false;
            }
        };

        _menu.OnCoordsYChanged += (text) =>
        {
            if (int.TryParse(text, out int coord)) //check if valid integer, if not, purge!
            {
                if (Math.Abs(coord) < teleComp.MaxRange) //limit maximum value, currently absolute coordinate value rather than actual range.
                {
                    coordY = coord;
                    coordYValid = true;
                    if (coordXValid)
                        _menu.UpdateTeleportSummary(ErrReady + CoordString(coordX, coordY)); //both are valid, so indicate ready to teleport
                }
                else
                {
                    _menu.SetCoordsY("");
                    _menu.UpdateTeleportSummary(ErrBigRange + " " + teleComp.MaxRange.ToString());
                    coordYValid = false;
                }
            }
            else
            {
                _menu.SetCoordsY("");
                _menu.UpdateTeleportSummary(ErrInsufficient);
                coordYValid = false;
            }
        };

        _menu.SendClicked += (link, send) =>
        { //for beacons have an if that is true if beacon selected and false if not. If true, use a seperate activate message.
            if (coordXValid == true && coordYValid == true) //require values to be input before teleport can be sent
                SendPredictedMessage(new TeleporterActivateMessage(new Vector2(coordX, coordY), send));
            else
                _menu.UpdateTeleportSummary(ErrInsufficient);
        };

        _menu.ReceiveClicked += (link, send) =>
        {
            if (coordXValid == true && coordYValid == true) //require values to be input before teleporter can be sent
                SendPredictedMessage(new TeleporterActivateMessage(new Vector2(coordX, coordY), send));
            else
                _menu.UpdateTeleportSummary(ErrInsufficient);
        };

    }

    public string CoordString(int x, int y)
    {
        return " (" + x.ToString() + ", " + y.ToString() + ")";
    }

}



