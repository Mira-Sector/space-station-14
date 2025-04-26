using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.WashingMachine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class WashingMachineComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan WashingTime;

    [ViewVariables, AutoNetworkedField, AutoPausedField, Access(typeof(WashingMachineSystem))]
    public TimeSpan WashingFinished;

    [ViewVariables, AutoNetworkedField, Access(typeof(WashingMachineSystem))]
    public WashingMachineState WashingMachineState;

    [DataField("slots")]
    public uint SlotCount = 8;

    [ViewVariables, AutoNetworkedField, Access(typeof(WashingMachineSystem))]
    public Dictionary<string, ItemSlot> Slots = new();
}
