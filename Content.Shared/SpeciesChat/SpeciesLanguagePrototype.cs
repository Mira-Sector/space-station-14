namespace Content.Shared.SpeciesChat;

public sealed partial class SpeciesLanguageComponent : Component
{
    /// <summary>
    /// Every language they can speak for others to hear if they can understand the language
    /// </summary>
    [DataField]
    public List<SpeciesChannelPrototype> SpokenLanguages = new List<SpeciesChannelPrototype>();

    /// <summary>
    /// Can hear these languages
    /// If unset uses every language they can speak instead
    /// </summary>
    [DataField]
    public List<SpeciesChannelPrototype> UnderstoodLanguages = new List<SpeciesChannelPrototype>();
}
