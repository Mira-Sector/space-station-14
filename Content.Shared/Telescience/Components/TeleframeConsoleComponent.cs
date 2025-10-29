using Content.Shared.DeviceLinking;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Telescience.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleframeConsoleComponent : Component
{
    /// <summary>
    /// The analyzer entity the console is linked.
    /// Can be null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public EntityUid? LinkedTeleframe;

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
    public HashSet<TeleportPoint> BeaconList = new(); //times switching between TeleportPoint and NetCoordinates: 4
}
