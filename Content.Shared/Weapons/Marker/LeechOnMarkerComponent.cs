using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Content.Shared.EntityEffects;

namespace Content.Shared.Weapons.Marker;

/// <summary>
/// Applies leech upon hitting a damage marker target.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LeechOnMarkerComponent : Component
{
    // TODO: Can't network damagespecifiers yet last I checked.
    //[ViewVariables(VVAccess.ReadWrite)]
    [DataField("leechEffects", required: true, serverOnly: true)]
    public List<EntityEffect> Effects = new(0);

    [DataField("targetSelf")]
    public bool TargetSelf = true;
}
