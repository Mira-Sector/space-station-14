using Content.Shared.Storage;
using Robust.Shared.Audio;
namespace Content.Server.Storage.Components;

public interface ISpawnItems
{
    /// <summary>
    ///     The list of entities to spawn, with amounts and orGroups.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Items { get; set; }

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound { get; set; }

    /// <summary>
    ///     How many uses before the item should delete itself.
    /// </summary>
    [DataField]
    public int Uses { get; set; }
}
