using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Messages;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Network;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerArcadeSystem : SharedRacerArcadeSystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    private IClydeWindow? _editingWindow = null;

    public override void Initialize()
    {
        base.Initialize();

        _net.Disconnect += OnClientDisconnected;

        SubscribeNetworkEvent<RacerArcadeEditorStartMessage>(OnEditorStart);
        SubscribeNetworkEvent<RacerArcadeEditorStopMessage>(OnEditorStop);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _net.Disconnect -= OnClientDisconnected;
    }

    private void OnClientDisconnected(object? sender, NetDisconnectedArgs args)
    {
        StopEditingSession();
    }

    private void OnEditorStart(RacerArcadeEditorStartMessage args)
    {
        StartEditingSession(args.Data);
    }

    private void OnEditorStop(RacerArcadeEditorStopMessage args)
    {
        StopEditingSession();
    }

    [PublicAPI]
    public void StartEditingSession(RacerGameStageEditorData? data = null)
    {
        if (_editingWindow != null)
            return;

        _editingWindow = _clyde.CreateWindow(new WindowCreateParameters()
        {
            Title = Loc.GetString("racer-editor-title")
        });
        _editingWindow.DisposeOnClose = true;
        _editingWindow.Destroyed += _ => StopEditingSession();

        var root = _userInterface.CreateWindowRoot(_editingWindow);
        var control = new RacerEditorControl();
        root.AddChild(control);

        data ??= RacerGameStageEditorData.Default;
        control.SetEditorData(data);
    }

    [PublicAPI]
    public void StopEditingSession()
    {
        if (_editingWindow == null || _editingWindow.IsDisposed)
            return;

        _editingWindow.Dispose();
        _editingWindow = null;

        if (!_net.IsConnected)
            return;

        var ev = new RacerArcadeEditorExitedMessage();
        RaiseNetworkEvent(ev);
    }
}
