using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Arcade.Racer;

[Serializable, NetSerializable]
public sealed partial class RacerGameStageEditorData : BaseRacerGameStage
{
    // used for the default state of the editor
    // should be the basics so shit doesnt crash and we can modify to what we actually want
    public static readonly RacerGameStageEditorData Default = new()
    {
        Sky = new()
        {
            Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Racer/skies.rsi"), "factory"),
        },
        Graph = new()
        {
            StartingNode = null
        }
    };
}
