using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.StationAi;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStationAiSystem))]
public sealed partial class StationAiCanHackComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string CurrencyPrototype = "Power";

    [DataField]
    public string ActionId = "ActionStationAiShop";
    public EntityUid? Action;
}
