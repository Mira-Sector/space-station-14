using Content.Shared.DeviceLinking;
using Content.Shared.Radio;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;


namespace Content.Shared.Telescience.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]

public sealed partial class TeleframeConsoleComponent : Component
{
    /// <summary>
    /// The analyzer entity the console is linked.
    /// Can be null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public NetEntity? LinkedTeleframe;

    /// <summary>
    /// largest coordinate value allowed for teleporting.
    /// </summary>
    [DataField]
    public int? MaxRange = null;

    [DataField]
    public SoundSpecifier? TeleportRechargedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg", AudioParams.Default.WithVolume(-4f));

    /// <summary>
    /// The machine linking port for the Teleframe
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

    /// <summary>
    /// AnnouncementChannel gets upset if it's nullable so this variable decides whether the console will actually speak or not
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool NoRadio = true;

}
