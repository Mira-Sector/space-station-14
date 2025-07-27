using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryUserInterfaceLinkedSourceComponent : Component
{
    public static readonly ProtoId<SourcePortPrototype> SourcePort = "SurgeryUiSource";

    [DataField, AutoNetworkedField]
    public EntityUid? Sink;
}
