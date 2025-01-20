using Content.Shared.Implants;
using Content.Shared.Traits;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Traits;

[UsedImplicitly]
public sealed partial class AddImplantSpecial : TraitSpecial
{
    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EntityPrototype>))]
    public HashSet<string> Implants { get; private set; } = new();

    public override void TraitAdded(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var implantSystem = entMan.System<SharedSubdermalImplantSystem>();
        implantSystem.AddImplants(mob, Implants);
    }
}
