using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VocalOrganComponent : Component, IVocalComponent
{
    [DataField, AutoNetworkedField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? Sounds { get; set; }

    [DataField, AutoNetworkedField]
    public EntProtoId ScreamId { get; set; } = "Scream";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Wilhelm { get; set; } = new SoundPathSpecifier("/Audio/Voice/Human/wilhelm_scream.ogg");

    [DataField, AutoNetworkedField]
    public float WilhelmProbability { get; set; } = 0.0002f;

    [DataField, AutoNetworkedField]
    public EntProtoId? ScreamAction { get; set; } = "ActionScream";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? ScreamActionEntity { get; set; }

    [ViewVariables, AutoNetworkedField]
    public EmoteSoundsPrototype? EmoteSounds { get; set; }
}
