using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Messages;
using Content.Shared.Arcade.Racer.Systems;
using Robust.Server.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using JetBrains.Annotations;

namespace Content.Server.Arcade.Racer.Systems;

public sealed partial class RacerArcadeSystem : SharedRacerArcadeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private readonly Dictionary<ICommonSession, (string, ResPath)> _editingSessions = [];

    public override void Initialize()
    {
        base.Initialize();

        _player.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeNetworkEvent<RacerArcadeEditorExitedMessage>(OnEditorExited);
        SubscribeNetworkEvent<RacerArcadeEditorSaveMessage>(OnEditorSave);
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

    /*
     * jesus christ this is an actual disaster
     * i just want to serialize this damnit
     *
     * replace this asap as soon as there is a way to do this that isnt fucking abhorant
    */
    private void OnEditorSave(RacerArcadeEditorSaveMessage args, EntitySessionEventArgs eventArgs)
    {
        if (!_editingSessions.TryGetValue(eventArgs.SenderSession, out var saveData))
            return;

        var (id, resPath) = saveData;
        var toProto = args.Data.ToPrototype(id);
        if (!PrototypeMan.TryGetKindFrom(toProto.GetType(), out var protoName))
            return;

        var data = (MappingDataNode)_serialization.WriteValue(toProto, notNullableOverride: true);
        data.InsertAt(0, "type", new ValueDataNode(protoName));
        var yaml = new SequenceDataNode
        {
            data
        };

        var path = resPath.ToRootedPath();
        _resource.UserData.CreateDir(path.Directory);
        using var writer = _resource.UserData.OpenWriteText(path);
        yaml.Write(writer);
    }

    [PublicAPI]
    public void StartEditingSession(ICommonSession session, string id, ResPath path, RacerGameStageEditorData? data = null)
    {
        if (session.Status != SessionStatus.InGame)
            return;

        if (_editingSessions.ContainsKey(session))
            StopEditingSession(session);

        data ??= RacerGameStageEditorData.Default;

        _editingSessions[session] = (id, path);
        var ev = new RacerArcadeEditorStartMessage(data);
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
