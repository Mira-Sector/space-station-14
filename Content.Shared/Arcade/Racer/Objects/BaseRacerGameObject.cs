using Content.Shared.PolygonRenderer;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Objects;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class BaseRacerGameObject
{
    [DataField(required: true)]
    public ProtoId<PolygonModelPrototype> Model;

    [ViewVariables]
    public Vector3 Position;
}
