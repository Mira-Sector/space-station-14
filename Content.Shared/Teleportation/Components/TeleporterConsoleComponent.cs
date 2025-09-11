using Content.Shared.DeviceLinking;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]

public sealed partial class TeleporterConsoleComponent : Component
{
    /// <summary>
    /// The analyzer entity the console is linked.
    /// Can be null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public NetEntity? LinkedTeleporter;

    /// <summary>
    /// largest coordinate value allowed for teleporting.
    /// </summary>
    [DataField]
    public int MaxRange = 20000;

    [DataField]
    public SoundSpecifier? TeleportRechargedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    /// <summary>
    /// The machine linking port for the Teleporter
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "TeleportSender";

    [DataField, ViewVariables, AutoNetworkedField]
    public HashSet<TeleportPoint> BeaconList = new();

    /// <summary>
    /// The radio channel that that teleporation events are broadcast to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Science";

}
