using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// If non zero the forces all players to spawn at arrivals for the set duration then cryo is enabled.
    /// </summary>
    public static readonly CVarDef<int> ForceArrivals =
        CVarDef.Create("shuttle.force_arrivals", 0, CVar.SERVERONLY);
}
