using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.Overlays;

/// <summary>
/// Shows a healthy icon on mobs.
/// </summary>
public sealed class ShowHealthIconsSystem : EquipmentHudSystem<ShowHealthIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;

    [ViewVariables]
    public HashSet<string> DamageContainers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<BodyComponent, GetStatusIconsEvent>(OnBodyGetStatusIconsEvent);
        SubscribeLocalEvent<ShowHealthIconsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowHealthIconsComponent> component)
    {
        base.UpdateInternal(component);

        foreach (var damageContainerId in component.Components.SelectMany(x => x.DamageContainers))
        {
            DamageContainers.Add(damageContainerId);
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        DamageContainers.Clear();
    }

    private void OnHandleState(Entity<ShowHealthIconsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    private void OnGetStatusIconsEvent(Entity<DamageableComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        if (!IsValid(entity.Comp))
            return;

        args.StatusIcons.AddRange(DecideHealthIcons(entity.Owner, entity.Comp.Damage, entity.Comp.DamageContainerID, entity.Comp.RottingIcon, entity.Comp.HealthIcons));
    }

    private void OnBodyGetStatusIconsEvent(Entity<BodyComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        DamageSpecifier totalDamage = new ();

        List<string?> damageContainers = new ();
        List<ProtoId<HealthIconPrototype>> rottingIcons = new ();
        List<Dictionary<MobState, ProtoId<HealthIconPrototype>>> healthIcons = new ();

        foreach (var (part, partComp) in _body.GetBodyChildren(entity))
        {
            if (!TryComp<DamageableComponent>(part, out var partDameagableComp))
                continue;

            if (!IsValid(partDameagableComp))
                continue;

            totalDamage += partDameagableComp.Damage * partComp.OverallDamageScale;
            damageContainers.Add(partDameagableComp.DamageContainerID);
            rottingIcons.Add(partDameagableComp.RottingIcon);
            healthIcons.Add(partDameagableComp.HealthIcons);
        }

        args.StatusIcons.AddRange(DecideHealthIcons(entity.Owner, totalDamage, GetMostFrequentItem(damageContainers), GetMostFrequentItem(rottingIcons), GetMostFrequentItem(healthIcons)));
    }

    private static T GetMostFrequentItem<T>(IEnumerable<T> data)
    {
        if (data == null || !data.Any())
        {
            throw new ArgumentException($"{typeof(T)} is null or empty.");
        }

        var mostFrequent = data
            .GroupBy(item => item)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        return mostFrequent != null
            ? mostFrequent.Key
            : throw new InvalidOperationException($"Unexpected error during processing {typeof(T)}.");
    }

    private bool IsValid(DamageableComponent component)
    {
        return component.DamageContainerID != null && DamageContainers.Contains(component.DamageContainerID);
    }

    private IReadOnlyList<HealthIconPrototype> DecideHealthIcons(EntityUid uid, DamageSpecifier damage, string? damageContainer, ProtoId<HealthIconPrototype> rottingIconProto, Dictionary<MobState, ProtoId<HealthIconPrototype>> healthIcons)
    {
        var result = new List<HealthIconPrototype>();

        // Here you could check health status, diseases, mind status, etc. and pick a good icon, or multiple depending on whatever.
        if (damageContainer == "Biological")
        {
            if (TryComp<MobStateComponent>(uid, out var state))
            {
                // Since there is no MobState for a rotting mob, we have to deal with this case first.
                if (HasComp<RottingComponent>(uid) && _prototypeMan.TryIndex(rottingIconProto, out var rottingIcon))
                    result.Add(rottingIcon);
                else if (healthIcons.TryGetValue(state.CurrentState, out var value) && _prototypeMan.TryIndex(value, out var icon))
                    result.Add(icon);
            }
        }

        return result;
    }
}
