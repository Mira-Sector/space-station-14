using Robust.Shared.Audio;

namespace Content.Server.Supermatter.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SupermatterAudioComponent : Component
{
    [ViewVariables]
    public bool DelaminationSounds;

    [DataField]
    public SoundSpecifier? NormalLoop;

    [DataField]
    public SoundSpecifier? DelaminationLoop;

    [DataField]
    public TimeSpan MinPulseSoundDelay;

    [DataField]
    public TimeSpan MaxPulseSoundDelay;

    [ViewVariables, AutoPausedField]
    public TimeSpan NextPulseSound;

    [DataField]
    public SoundSpecifier? NormalPulse;

    [DataField]
    public SoundSpecifier? DelaminationPulse;
}
