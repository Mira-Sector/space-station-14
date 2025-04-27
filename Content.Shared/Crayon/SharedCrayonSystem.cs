using Content.Shared.Dyable;

namespace Content.Shared.Crayon;

[Virtual]
public abstract class SharedCrayonSystem : EntitySystem
{
    protected static void OnDyeGetColor(EntityUid uid, SharedCrayonComponent component, GetDyableColorsEvent args)
    {
        args.Color = component.Color;
        args.Handled = true;
    }
}
