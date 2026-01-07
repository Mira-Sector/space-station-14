using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Messages;

[Serializable, NetSerializable]
public sealed partial class RacerArcadeEditorStartMessage(RacerGameStageEditorData data) : EntityEventArgs
{
    public readonly RacerGameStageEditorData Data = data;
}
