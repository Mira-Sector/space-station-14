using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.SpeciesChat;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpeciesLanguageComponent : Component
{
    /// <summary>
    /// Every language they can speak for others to hear if they can understand the language
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesChannelPrototype>)), AutoNetworkedField]
    public HashSet<string> SpokenLanguages = new();

    /// <summary>
    /// Can hear these languages
    /// If unset uses every language they can speak instead
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesChannelPrototype>)), AutoNetworkedField]
    public HashSet<string> UnderstoodLanguages = new();
}
