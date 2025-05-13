using Content.Shared.Storage;
using Robust.Shared.Audio;

namespace Content.Server.Storage.Components
{
    /// <summary>
    ///     Spawns items when used in hand.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnItemsOnUseComponent : Component, ISpawnItems
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
}
