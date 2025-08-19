using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, AutoGenerateComponentState(true)]

public sealed partial class TeleporterConsoleComponent : Component
{
    /// <summary>
    /// The analyzer entity the console is linked.
    /// Can be null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? LinkedTeleporter;

    [DataField]
    public SoundSpecifier? TeleportRechargedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public SoundSpecifier? TeleportBeginSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    /// <summary>
    /// The machine linking port for the Teleporter
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> LinkingPort = "TeleportSender";
}
