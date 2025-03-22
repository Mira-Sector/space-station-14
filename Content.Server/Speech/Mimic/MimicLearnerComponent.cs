namespace Content.Server.Speech.Mimic;

[RegisterComponent]
public sealed partial class MimicLearnerComponent : Component
{
    /// <summary>
    /// How likely we are to learn a new phrase once weve heard it long term
    /// </summary>
    [DataField]
    public float LongTermLearningChance = 0.1f;

    /// <summary>
    /// How likely we are to learn a new phrase once weve heard it for the current round
    /// </summary>
    [DataField]
    public float CurrentRoundLearningChance = 0.5f;

    /// <summary>
    /// What probability to add for a phrase we just heard for long term storage
    /// </summary>
    [DataField]
    public float LongTermPhraseProb = 0.0005f;

    /// <summary>
    /// What probability to add for a phrase we just heard for the current round
    /// </summary>
    [DataField]
    public float CurrentRoundPhraseProb = 0.02f;
}
