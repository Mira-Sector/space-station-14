using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Add order to database.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleAddOrderMessage : BoundUserInterfaceMessage
{
    public string Requester;
    public string Reason;
    public ProtoId<CargoProductPrototype> CargoProductId;
    public int Amount;

    public CargoConsoleAddOrderMessage(string requester, string reason, ProtoId<CargoProductPrototype> cargoProductId, int amount)
    {
        Requester = requester;
        Reason = reason;
        CargoProductId = cargoProductId;
        Amount = amount;
    }
}
