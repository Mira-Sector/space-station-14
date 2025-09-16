using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.Parallax.Data;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SolidColorParallaxTextureSource : IParallaxTextureSource
{
    Task<Texture> IParallaxTextureSource.GenerateTexture(CancellationToken cancel)
    {
        return Task.FromResult(Texture.White);
    }
}

