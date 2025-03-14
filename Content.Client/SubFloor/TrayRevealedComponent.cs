using Content.Shared.SubFloor;

namespace Content.Client.SubFloor;

/// <summary>
/// Added clientside if an entity is revealed for TRay.
/// </summary>
[RegisterComponent]
public sealed partial class TrayRevealedComponent : Component
{

}

public sealed class TrayCanRevealEvent : CancellableEntityEventArgs
{
    public Entity<TrayScannerComponent> Tray;

    public TrayCanRevealEvent(Entity<TrayScannerComponent> tray)
    {
        Tray = tray;
    }
}
