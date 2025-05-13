using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech.Components;

[RegisterComponent]
public sealed partial class VocalBodyPartComponent : Component
{
    [DataField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>> Sounds = new();
}
