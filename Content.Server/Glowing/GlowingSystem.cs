using Content.Shared.StatusEffect;
//using Content.Shared.Glowing;
using Robust.Shared.Prototypes;

namespace Content.Server.Glowing;

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
    public void DoGlow(EntityUid uid, TimeSpan time, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        if (!_statusEffectsSystem.HasStatusEffect(uid, GlowingKey, status))
        {
            _statusEffectsSystem.TryAddStatusEffect<GlowingComponent>(uid, GlowingKey, time, false, status);
        }
        else
        {
            _statusEffectsSystem.TryAddTime(uid, GlowingKey, time, status);
        }
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
    public void OnGlowInit(EntityUid uid, GlowingComponent component, ComponentInit args)
    {
        var light = _light.EnsureLight(uid);

        _light.SetRadius(uid, component.Radius, light);
        _light.SetColor(uid, component.Color, light);
        _light.SetCastShadows(uid, false, light);
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

    public void OnGlowShutdown(EntityUid uid, GlowingComponent component, ComponentShutdown args)
    {
        _light.RemoveLightDeferred(uid);
    }
}


