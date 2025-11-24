using Content.Shared.PolygonRenderer;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.Racer.Objects.Vehicles;

[Serializable, NetSerializable]
public sealed partial class RacerGameVehiclePlayer : BaseRacerGameVehicle
{
    private static readonly ProtoId<PolygonModelPrototype> ModelId = "RacerVehiclePlayer";

    public RacerGameVehiclePlayer()
    {
        Model = ModelId;
    }
}
