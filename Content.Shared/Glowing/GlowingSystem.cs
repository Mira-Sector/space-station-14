using Content.Shared.StatusEffect;

namespace Content.Shared.Glowing;

public sealed class GlowingSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public const string GlowingKey = "Glowing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlowingComponent, ComponentInit>(OnGlowInit);
        SubscribeLocalEvent<GlowingComponent, ComponentShutdown>(OnGlowShutdown);
    }

    public void DoGlow(Entity<StatusEffectsComponent?> ent, TimeSpan time)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(ent.Owner, GlowingKey, ent.Comp))
            _statusEffectsSystem.TryAddStatusEffect<GlowingComponent>(ent.Owner, GlowingKey, time, false, ent.Comp);
        else
            _statusEffectsSystem.TryAddTime(ent.Owner, GlowingKey, time, ent.Comp);
    }

    public void TryRemoveGlowTime(EntityUid uid, TimeSpan timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, GlowingKey, timeRemoved);
    }

    public void TryRemoveGlow(EntityUid uid)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(uid, GlowingKey);
    }

    ///<summary>
    /// On component initiating, create generic glowing effect, that can be updated later.
    ///</summary>
    public void OnGlowInit(Entity<GlowingComponent> ent, ref ComponentInit args)
    {
        var light = _light.EnsureLight(ent.Owner);

        _light.SetRadius(ent.Owner, ent.Comp.Radius, light);
        _light.SetColor(ent.Owner, ent.Comp.Color, light);
        _light.SetCastShadows(ent.Owner, false, light);
    }

    public void UpdateGlowRadius(EntityUid uid, float radius)
    {
        _light.SetRadius(uid, radius);
    }

    public void UpdateGlowColor(EntityUid uid, Color color)
    {
        _light.SetColor(uid, color);
    }

    public void UpdateGlowEnergy(EntityUid uid, float energy)
    {
        _light.SetEnergy(uid, energy);
    }

    public void OnGlowShutdown(Entity<GlowingComponent> ent, ref ComponentShutdown args)
    {
        _light.RemoveLightDeferred(ent.Owner);
    }
}
