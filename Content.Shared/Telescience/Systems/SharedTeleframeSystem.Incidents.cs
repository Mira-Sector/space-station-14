using System.Diagnostics.CodeAnalysis;
using Content.Shared.Emag.Systems;
using Content.Shared.Telescience.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeIncidents()
    {
        SubscribeLocalEvent<TeleframeIncidentLiableComponent, TeleframeTeleportedAllEvent>(OnIncidentTeleported);
        SubscribeLocalEvent<TeleframeIncidentLiableComponent, TeleframeTeleportFailedEvent>(OnIncidentFailed);

        SubscribeLocalEvent<TeleframeIncidentLiableComponent, GotEmaggedEvent>(OnIncidentEmagged);
    }

    private void OnIncidentTeleported(Entity<TeleframeIncidentLiableComponent> ent, ref TeleframeTeleportedAllEvent args)
    {
        if (!TryRollForIncident(ent, out var severity))
            return;

        //TODO: raise TeleframeIncidentEvent and TeleframeUserIncidentEvent when incidents are refactored

        TeleframeIncidentExplode(ent, severity!.Value);
    }

    private void OnIncidentFailed(Entity<TeleframeIncidentLiableComponent> ent, ref TeleframeTeleportFailedEvent args)
    {
        if (!TryRollForIncident(ent, out var severity))
            return;

        TeleframeIncidentExplode(ent, severity!.Value);
    }

    /// <summary>
    /// Adds the emag flag to the Teleframe, makes the Teleframe more dangerous, cumulative with any other effect that does that.
    /// </summary>
    private void OnIncidentEmagged(Entity<TeleframeIncidentLiableComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private bool TryRollForIncident(Entity<TeleframeIncidentLiableComponent> ent, [NotNullWhen(true)] out float? severity)
    {
        var roll = Random.NextFloat();

        var chance = _emag.CheckFlag(ent.Owner, EmagType.Interaction) ? ent.Comp.EmagIncidentChance + ent.Comp.IncidentChance : ent.Comp.IncidentChance;
        var multiplier = _emag.CheckFlag(ent.Owner, EmagType.Interaction) ? ent.Comp.EmagIncidentMultiplier + ent.Comp.IncidentMultiplier : ent.Comp.IncidentMultiplier;

        if (roll < chance)
        {
            severity = Random.NextFloat() * multiplier;
            return true;
        }

        severity = null;
        return false;
    }

    protected virtual void TeleframeIncidentExplode(Entity<TeleframeIncidentLiableComponent> ent, float severity)
    {
    }
}
