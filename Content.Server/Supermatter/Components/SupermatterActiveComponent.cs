using Content.Shared.Whitelist;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterActiveComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Active;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ActiveVV
    {
        get => Active;
        set
        {
            var entMan = IoCManager.Resolve<EntityManager>();
            var supermatterSystem = entMan.System<SupermatterSystem>();
            supermatterSystem.ActivateSupermatter((this.Owner, this), value);
        }
    }

    [DataField]
    public EntityWhitelist ActivationBlackList = new();
}
