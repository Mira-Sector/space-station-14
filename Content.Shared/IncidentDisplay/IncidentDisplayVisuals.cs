using Robust.Shared.Serialization;

namespace Content.Shared.IncidentDisplay;

[Serializable, NetSerializable]
public enum IncidentDisplayVisuals : byte
{
    Screen,
    Relative,
    Type,
    Hundreds,
    Tens,
    Units
}

[Serializable, NetSerializable]
public enum IncidentDisplayScreenVisuals : byte
{
    Normal,
    Advertisement,
    UnPowered,
    Broken
}
