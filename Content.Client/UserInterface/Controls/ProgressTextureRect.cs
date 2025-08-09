using System.Numerics;
using Content.Client.UserInterface.Systems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls
{
    public sealed class ProgressTextureRect : TextureRect
    {
        [ViewVariables]
        public float Progress;

        [ViewVariables]
        public ProgressDirection ProgressDirection { get; set; } = ProgressDirection.Top;

        private readonly ProgressColorSystem _progressColor;

        public ProgressTextureRect()
        {
            _progressColor = IoCManager.Resolve<IEntityManager>().System<ProgressColorSystem>();
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var dims = Texture != null ? GetDrawDimensions(Texture) : UIBox2.FromDimensions(Vector2.Zero, PixelSize);

            switch (ProgressDirection)
            {
                case ProgressDirection.Top:
                    dims.Top = Math.Max(dims.Bottom - dims.Bottom * Progress, 0);
                    break;
                case ProgressDirection.Bottom:
                    dims.Bottom *= Progress;
                    break;
                case ProgressDirection.Left:
                    dims.Left = Math.Max(dims.Right - dims.Right * Progress, 0);
                    break;
                case ProgressDirection.Right:
                    dims.Right *= Progress;
                    break;
            }

            handle.DrawRect(dims, _progressColor.GetProgressColor(Progress));

            base.Draw(handle);
        }
    }

    public enum ProgressDirection : byte
    {
        Top,
        Bottom,
        Left,
        Right
    }
}
