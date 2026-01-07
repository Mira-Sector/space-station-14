using Content.Shared.Arcade.Racer.Stage;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class BaseRacerGameStage
{
    [DataField(required: true)]
    public RacerGameStageSkyData Sky = default!;

    [DataField(required: true)]
    public RacerArcadeStageGraph Graph = default!;

    public RacerGameStagePrototype ToPrototype(string id)
    {
        // no i dont care
        // fuck off
        // i just want to serialize this to disk
#pragma warning disable RA0039
        var proto = new RacerGameStagePrototype()
        {
            ID = id,
            Sky = Sky,
            Graph = Graph
        };
#pragma warning restore RA0039
        return proto;
    }

    public RacerGameStageEditorData ToEditorData()
    {
        var data = new RacerGameStageEditorData()
        {
            Sky = Sky,
            Graph = Graph
        };
        return data;
    }
}
