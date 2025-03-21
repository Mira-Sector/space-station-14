namespace Content.Server.Speech.Mimic;

[RegisterComponent]
public sealed partial class MimicLearnerComponent : Component
{
    /// <summary>
    /// How likely we are to learn a new phrase once weve heard it
    /// </summary>
    [DataField]
    public float LearningChance = 0.5f;

    /// <summary>
    /// What probability to add for a phrase we just heard
    /// </summary>
    [DataField]
    public float PhraseProb = 0.002f;
}
