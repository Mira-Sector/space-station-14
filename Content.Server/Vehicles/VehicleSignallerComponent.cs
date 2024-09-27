using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Vehicles;

[RegisterComponent]
public sealed partial class VehicleSignallerComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>))]
    public string Port = "Pressed";
}
