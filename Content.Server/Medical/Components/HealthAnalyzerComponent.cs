using Content.Server.BaseAnalyzer;
using Content.Shared.MedicalScanner;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.Components;

/// <inheritdoc/>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class HealthAnalyzerComponent : BaseAnalyzerComponent
{
    /// <inheritdoc/>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public override TimeSpan NextUpdate { get; set; } = TimeSpan.Zero;

    [DataField]
    public HealthAnalyzerType AnalyzerType = HealthAnalyzerType.Body;
}
