namespace Content.Client.HealthAnalyzer.UI;

public interface IHealthAnalyzerBodyButton
{
    object? Identifier { get; set; }
    EntityUid? Owner { get; set; }
}
