using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Coughing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CougherComponent : Component
{
    [DataField]
    public ProtoId<EmotePrototype> CoughingEmote = "Cough";

    [DataField]
    public TimeSpan MinCoughDelay = TimeSpan.FromSeconds(1f);

    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastCough;
}
