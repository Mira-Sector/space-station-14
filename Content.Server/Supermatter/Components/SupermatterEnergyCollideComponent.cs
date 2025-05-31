using Content.Shared.Whitelist;

namespace Content.Server.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterEnergyCollideComponent : Component
{
    [DataField]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public EntityWhitelist Blacklist = new();

    [DataField(required:true)]
    public string OurFixtureId = string.Empty;

    [DataField]
    public float BaseEnergyOnCollide = 20f;
}
