using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Server.Arcade.Racer.Systems;
using Content.Shared.Arcade.Racer;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.Server.Arcade.Racer;

[AdminCommand(AdminFlags.Mapping)]
public sealed class RacerEditorCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceManager _resource = default!;
    [Dependency] private readonly RacerArcadeSystem _racer = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public override string Command => "racer_editor";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromOptions(CompletionHelper.UserFilePath(args[0], _resource.UserData)
                .Concat(CompletionHelper.ContentFilePath(args[0], _resource))
            ),
            _ => CompletionResult.Empty
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        if (shell.Player is not { } player)
            return;

        if (!ResPath.IsValidPath(args[0]))
            return;

        var path = new ResPath(args[0]);
        if (!TryGetYaml(path, out var yaml))
            return;

        if (!_prototype.TryGetKindFrom<RacerGameStagePrototype>(out var expectedTypeNode))
            return;

        var sequenceNode = yaml.Documents[0].RootNode.ToDataNodeCast<SequenceDataNode>();
        var node = sequenceNode.First().ToYamlNode().ToDataNodeCast<MappingDataNode>();
        if (!node.TryGet<ValueDataNode>("type", out var typeNode) || typeNode.Value != expectedTypeNode)
            return;

        var proto = _serialization.Read<RacerGameStagePrototype>(node, notNullableOverride: true);

        var data = proto.ToEditorData();
        _racer.StartEditingSession(player, proto.ID, path, data);
    }

    private bool TryGetYaml(ResPath path, [NotNullWhen(true)] out YamlStream? yaml)
    {
        // prioritise user data
        if (_resource.UserData.Exists(path))
        {
            try
            {
                var stream = _resource.UserData.Open(path, FileMode.Open);
                var reader = new StreamReader(stream, EncodingHelpers.UTF8);
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                yaml = yamlStream;
                stream.Dispose();
                return true;
            }
            catch
            {
                yaml = null;
                return false;
            }
        }
        else if (_resource.ContentFileExists(path))
        {
            yaml = _resource.ContentFileReadYaml(path);
            return true;
        }
        else
        {
            yaml = null;
            return false;
        }
    }
}
