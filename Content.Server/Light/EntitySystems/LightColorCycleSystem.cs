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

    Dictionary<(int, TimeSpan), LightColorCycle> Lights = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightColorCycleComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<LightColorCycleComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<LightColorCycleComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var _lights = Lights;

        foreach (var ((states, updateRate), light) in _lights)
        {
            if (light.NextUpdate > _timing.CurTime)
                continue;

            light.NextUpdate += updateRate;

            light.CurrentState++;

            if (light.CurrentState >= states)
                light.CurrentState = 1;

            foreach (var (uid, component) in light.Entities)
            {
                if (!component.IsPowered)
                    continue;

                component.CurrentState = light.CurrentState;

                var state = component.States[light.CurrentState];

                _appearance.SetData(uid, LightColorCycleVisuals.State, state.State);
                _pointLight.SetColor(uid, state.Color);
            }
        }
    }

    private void OnInit(EntityUid uid, LightColorCycleComponent component, MapInitEvent args)
    {
        var first = component.States[1];
        component.CurrentState = 1;

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
                AddLight(uid, component);
                return;
            }
        }

        component.IsPowered = true;
        _appearance.SetData(uid, LightColorCycleVisuals.State, first.State);
        _pointLight.SetColor(uid, first.Color);
        _pointLight.SetEnabled(uid, true);
        AddLight(uid, component);
    }

    private void OnRemove(EntityUid uid, LightColorCycleComponent component, ComponentRemove args)
    {
        RemoveLight(uid, component);
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

    private void AddLight(EntityUid uid, LightColorCycleComponent component)
    {
        var states = (component.States.Count, component.Speed);
        if (Lights.ContainsKey(states))
        {
            var light = Lights[states];
            light.Entities.Add((uid, component));
        }
        else
        {
            var nextUpdate = _timing.CurTime + component.Speed;
            HashSet<Entity<LightColorCycleComponent>> entities = new();
            entities.Add((uid, component));
            var lightState = new LightColorCycle(nextUpdate, entities);

            Lights.Add(states, lightState);
        }
    }

    private void RemoveLight(EntityUid uid, LightColorCycleComponent component)
    {
        var states = (component.States.Count, component.Speed);
        var entities = Lights[states].Entities;
        entities.Remove((uid, component));

        if (!entities.Any())
            Lights.Remove(states);
    }
}

public class LightColorCycle
{
    public TimeSpan NextUpdate;
    public int CurrentState;
    public HashSet<Entity<LightColorCycleComponent>> Entities = new();

    public LightColorCycle(TimeSpan nextUpdate, HashSet<Entity<LightColorCycleComponent>> entities, int currentState = 1)
    {
        NextUpdate = nextUpdate;
        Entities = entities;
        CurrentState = currentState;
    }
}
