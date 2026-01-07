using Content.Shared.PolygonRenderer;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Arcade.Racer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RacerArcadeObjectModelComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<PolygonModelPrototype> Model;
}
