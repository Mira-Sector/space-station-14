using Content.Shared.Electrocution;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server.Electrocution;

[RegisterComponent]
public sealed partial class ElectrifiedComponent : SharedElectrifiedComponent
{
    /// <summary>
    /// When true - disables power if a window is present in the same tile
    /// </summary>
    [DataField]
    public bool NoWindowInTile = false;

    /// <summary>
    /// Indicates if the entity requires power to function
    /// </summary>
    [DataField]
    public bool RequirePower = true;

    /// <summary>
    /// Indicates if the entity uses APC power
    /// </summary>
    [DataField]
    public bool UsesApcPower = false;

    /// <summary>
    /// Identifier for the high voltage node.
    /// </summary>
    [DataField]
    public string? HighVoltageNode;

    /// <summary>
    /// Identifier for the medium voltage node.
    /// </summary>
    [DataField]
    public string? MediumVoltageNode;

    /// <summary>
    /// Identifier for the low voltage node.
    /// </summary>
    [DataField]
    public string? LowVoltageNode;

    [DataField]
    public Dictionary<NodeGroupID, ElectrocutionType>? WireDamageType = new();

    [DataField]
    public float SiemensCoefficient = 1f;
}
