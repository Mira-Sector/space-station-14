using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VocalOrganComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>> Sounds = new();
}
