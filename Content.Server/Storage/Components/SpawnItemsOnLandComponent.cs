using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed partial class SpawnItemsOnLandComponent : Component, ISpawnItems
{
    /// <inheritdoc/>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Items { get; set; }

    /// <inheritdoc/>
    [DataField]
    public SoundSpecifier? Sound { get; set; }

    /// <inheritdoc/>
    [DataField]
    public int Uses { get; set; } = 1;
}
