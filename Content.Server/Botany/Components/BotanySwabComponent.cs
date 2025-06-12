using System.Threading;

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
        /// SeedData from the first plant that got swabbed.
        /// </summary>
        public SeedData? SeedData;


    }
}
