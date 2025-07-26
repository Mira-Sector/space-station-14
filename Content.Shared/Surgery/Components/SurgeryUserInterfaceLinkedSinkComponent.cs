using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryUserInterfaceLinkedSinkComponent : Component
{
    public static readonly ProtoId<SinkPortPrototype> SinkPort = "SurgeryUiSink";

    [DataField, AutoNetworkedField]
    public EntityUid? Source;
}
