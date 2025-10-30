using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Messages;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using JetBrains.Annotations;

namespace Content.Server.Arcade.Racer;

public sealed partial class RacerArcadeSystem : SharedRacerArcadeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly Dictionary<ICommonSession, ResPath> _editingSessions = [];

    public override void Initialize()
    {
        base.Initialize();

        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<RacerArcadeEditorExitedMessage>(OnEditorExited);
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            StopEditingSession(args.Session);
    }

    private void OnEditorExited(RacerArcadeEditorExitedMessage args, EntitySessionEventArgs eventArgs)
    {
        StopEditingSession(eventArgs.SenderSession);
    }

    [PublicAPI]
    public void StartEditingSession(ICommonSession session, ResPath path)
    {
        if (session.Status != SessionStatus.InGame)
            return;

        if (_editingSessions.ContainsKey(session))
            StopEditingSession(session);

        _editingSessions[session] = path;
        var ev = new RacerArcadeEditorStartMessage();
        RaiseNetworkEvent(ev, session);
    }

    [PublicAPI]
    public void StopEditingSession(ICommonSession session)
    {
        if (!_editingSessions.Remove(session))
            return;

        if (session.Status != SessionStatus.InGame)
            return;

        var ev = new RacerArcadeEditorStopMessage();
        RaiseNetworkEvent(ev, session);
    }
}
