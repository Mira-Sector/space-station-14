using Content.Shared.Power;
using Content.Server.Power.Components;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.Light.EntitySystems;

public sealed class LightColorCycleSystem : SharedLightColorCycleSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightColorCycleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LightColorCycleComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LightColorCycleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            component.NextUpdate += component.Speed;

            if (!component.IsPowered)
                return;

            component.CurrentState++;

            if (component.CurrentState >= component.States.Count)
                component.CurrentState = 1;

            var state = component.States[component.CurrentState];

            _appearance.SetData(uid, LightColorCycleVisuals.State, state.State);
            _pointLight.SetColor(uid, state.Color);
        }
    }

    private void OnInit(EntityUid uid, LightColorCycleComponent component, ComponentInit args)
    {
        var first = component.States[1];
        component.CurrentState = 1;
        component.NextUpdate = _timing.CurTime + component.Speed;

        if (component.RequirePower)
        {
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiverComp))
            {
                RemComp<LightColorCycleComponent>(uid);
                return;
            }

            if (!powerReceiverComp.Powered)
            {
                component.IsPowered = false;
                _appearance.SetData(uid, LightColorCycleVisuals.State, component.UnpoweredState);
                _pointLight.SetEnabled(uid, false);
                return;
            }
        }

        component.IsPowered = true;
        _appearance.SetData(uid, LightColorCycleVisuals.State, first.State);
        _pointLight.SetColor(uid, first.Color);
        _pointLight.SetEnabled(uid, true);
    }

    private void OnPowerChanged(EntityUid uid, LightColorCycleComponent component, ref PowerChangedEvent args)
    {
        component.IsPowered = args.Powered;

        if (!args.Powered)
        {
            _appearance.SetData(uid, LightColorCycleVisuals.State, component.UnpoweredState);
            _pointLight.SetEnabled(uid, false);
            return;
        }

        _appearance.SetData(uid, LightColorCycleVisuals.State, component.States[component.CurrentState].State);
        _pointLight.SetColor(uid, component.States[component.CurrentState].Color);
        _pointLight.SetEnabled(uid, true);
    }
}
