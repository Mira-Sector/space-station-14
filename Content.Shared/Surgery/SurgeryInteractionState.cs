using Robust.Shared.Serialization;

namespace Content.Shared.Surgery;

[Serializable, NetSerializable]
public enum SurgeryInteractionState
{
    Failed, //cant be run
    Passed, //passed and something has been done
    DoAfter, //passed and we are waiting for a doafter to finish
    UserInterface //passed and we are waiting the ui to do something
}
