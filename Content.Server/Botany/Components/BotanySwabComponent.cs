using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.Botany
{
    /// <summary>
    /// Anything that can be used to cross-pollinate plants.
    /// </summary>
    [RegisterComponent]
    public sealed partial class BotanySwabComponent : Component
    {
        [DataField("swabDelay")]
        public float SwabDelay = 1f;

        /// <summary>
        /// Are the swab's contents replaced on swabbing, default true.
        /// </summary>
        [DataField]
        public bool Contaminate = true;

        /// <summary>
        /// Whether the swab is self-cleanable, default false
        /// </summary>
        [DataField]
        public bool Cleanable = false;

        /// <summary>
        /// Whether the swab can be used if it has no seed data, default true
        /// If false, a seperate may to provide seed data is required or the swab will be unusable
        /// </summary>
        [DataField]
        public bool Usable = true;

        /// <summary>
        /// SeedData from the first plant that got swabbed.
        /// </summary>
        public SeedData? SeedData;

        public SoundSpecifier SwabSound = new SoundPathSpecifier("/Audio/Effects/Footsteps/grass1.ogg");

        public SoundSpecifier CleanSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");
    }
}
