namespace Content.Server.Speech.Mimic;

[RegisterComponent]
public sealed partial class MimicSpeakerComponent : Component
{
    [DataField(required: true)]
    public TimeSpan MinDelay;

    [DataField(required: true)]
    public TimeSpan MaxDelay;

    [ViewVariables]
    public TimeSpan NextMessage;

    [DataField]
    public int CachedMessages = 10;

    /// <summary>
    /// The middle point of the probability
    /// If the total phrases known is below this we multiply by how close
    /// If we are over do nothing
    /// </summary>
    [DataField]
    public int MidPointProb = 500;

    [ViewVariables]
    public List<string> NextMessages = new();

    /// <summary>
    /// How likely we are to forget a phrase we just said long term
    /// </summary>
    [DataField]
    public float LongTermForgetChance = 0.005f;

    /// <summary>
    /// How likely we are to forget a phrase we just said in the current round
    /// </summary>
    [DataField]
    public float CurrentRoundForgetChance = 0.07f;

    /// <summary>
    /// What probability to remove for a phrase we just said for long term storage
    /// </summary>
    [DataField]
    public float LongTermForgetProb = 0.005f;

    /// <summary>
    /// What probability to remove for a phrase we just said for the current round
    /// </summary>
    [DataField]
    public float CurrentRoundForgetProb = 0.07f;
}
