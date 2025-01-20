using Content.Shared.SpeciesChat;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.SpeciesChat;

[RegisterComponent]
public sealed partial class RandomSpeciesLanguageComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesChannelPrototype>))]
    public HashSet<string> SpokenLanguages = new();

    [DataField]
    public uint SpokenLanguagesAmount = 1;

    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SpeciesChannelPrototype>))]
    public HashSet<string> UnderstoodLanguages = new();

    [DataField]
    public uint UnderstoodLanguagesAmount = 1;
}
