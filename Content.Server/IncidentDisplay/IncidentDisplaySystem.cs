using Content.Server.GameTicking.Events;
using Content.Server.Singularity.Events;
using Content.Shared.Destructible;
using Content.Shared.IncidentDisplay;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.IncidentDisplay;

public sealed partial class IncidentDisplaySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private Dictionary<IncidentDisplayType, int> KillCount = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<IncidentDisplayKillModifiedEvent>(OnKillModified);

        SubscribeLocalEvent<IncidentDisplayComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IncidentDisplayComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<IncidentDisplayComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<IncidentDisplayIncrementerComponent, EntityConsumedByEventHorizonEvent>(OnConsume);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IncidentDisplayComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Broken)
                continue;

            if (!_receiver.IsPowered(uid))
                continue;

            var wasAdvertising = false;

            if (component.Advertising)
            {
                if (component.AdvertisementEnd > _timing.CurTime)
                    continue;

                component.Advertising = false;
                wasAdvertising = true;
                component.NextType = _timing.CurTime;
            }
            else if (component.NextType > _timing.CurTime)
            {
                continue;
            }

            component.NextType += component.TimePerType;

            var types = component.SelectableTypes.ToList();
            var index = wasAdvertising ? 0 : types.IndexOf(component.CurrentType) + 1;

            if (index < types.Count)
            {
                component.CurrentType = types[index];
                UpdateState((uid, component));
                continue;
            }

            component.Advertising = true;
            component.AdvertisementEnd = _timing.CurTime + component.AdvertiseLength;
            component.CurrentType = types.First();
            UpdateScreen((uid, component), IncidentDisplayScreenVisuals.Advertisement);
            _appearance.SetData(uid, IncidentDisplayVisuals.Relative, IncidentDisplayRelative.None);
        }
    }

    internal void UpdateState(Entity<IncidentDisplayComponent> ent)
    {
        var kills = KillCount[ent.Comp.CurrentType];

        var hundreds = (int) (kills / 100 % 10);
        var tens = (int) (kills / 10 % 10);
        var units = (int) (kills % 10);

        UpdateScreen(ent, IncidentDisplayScreenVisuals.Normal);

        _appearance.SetData(ent, IncidentDisplayVisuals.Hundreds, hundreds);
        _appearance.SetData(ent, IncidentDisplayVisuals.Tens, tens);
        _appearance.SetData(ent, IncidentDisplayVisuals.Units, units);

        _appearance.SetData(ent, IncidentDisplayVisuals.Relative, ent.Comp.TypeRelative[ent.Comp.CurrentType]);
        ent.Comp.TypeRelative[ent.Comp.CurrentType] = IncidentDisplayRelative.None;
    }

    internal void UpdateScreen(Entity<IncidentDisplayComponent> ent, IncidentDisplayScreenVisuals screen)
    {
        _appearance.SetData(ent, IncidentDisplayVisuals.Screen, screen);

        if (!ent.Comp.RelativeColor.TryGetValue(ent.Comp.TypeRelative[ent.Comp.CurrentType], out var color) || color == null)
        {
            if (!ent.Comp.ScreenColor.TryGetValue(screen, out color) || color == null)
            {
                _lights.SetEnabled(ent, false);
                return;
            }
        }

        _lights.SetEnabled(ent, true);
        _lights.SetColor(ent, color.Value);
    }

    private void OnRoundStarting(RoundStartingEvent args)
    {
        KillCount.Clear();

        foreach (IncidentDisplayType type in Enum.GetValues(typeof(IncidentDisplayType)))
            KillCount.Add(type, 0);
    }

    private void OnKillModified(IncidentDisplayKillModifiedEvent args)
    {
        var query = EntityQueryEnumerator<IncidentDisplayComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.SelectableTypes.Contains(args.Type))
                continue;

            var relative = IncidentDisplayRelative.None;

            if (args.Modified > 0)
            {
                relative = IncidentDisplayRelative.Rising;
            }
            else
            {
                relative = IncidentDisplayRelative.Falling;
            }

            component.TypeRelative[args.Type] = relative;

            if (component.Broken)
                continue;

            if (component.Advertising)
                continue;

            if (component.CurrentType != args.Type)
                continue;

            if (!_receiver.IsPowered(uid))
                continue;

            component.NextType += component.TimePerType; // rub it in further
            UpdateState((uid, component));
        }
    }

    private void OnInit(Entity<IncidentDisplayComponent> ent, ref ComponentInit args)
    {
        foreach (var type in ent.Comp.SelectableTypes)
            ent.Comp.TypeRelative.Add(type, IncidentDisplayRelative.None);

        if (!_receiver.IsPowered(ent.Owner))
        {
            UpdateScreen(ent, IncidentDisplayScreenVisuals.UnPowered);
            return;
        }

        ent.Comp.NextType = _timing.CurTime;
        UpdateScreen(ent, IncidentDisplayScreenVisuals.Normal);

        UpdateState(ent);
    }

    private void OnBreak(Entity<IncidentDisplayComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.Broken = true;
        UpdateScreen(ent, IncidentDisplayScreenVisuals.Broken);
    }

    private void OnPowerChanged(Entity<IncidentDisplayComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            UpdateScreen(ent, IncidentDisplayScreenVisuals.UnPowered);
            return;
        }

        ent.Comp.Advertising = true;
        ent.Comp.NextType = _timing.CurTime;
        UpdateScreen(ent, IncidentDisplayScreenVisuals.Normal);

        UpdateState(ent);
    }

    private void OnConsume(Entity<IncidentDisplayIncrementerComponent> ent, ref EntityConsumedByEventHorizonEvent args)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Entity))
            return;

        KillCount[ent.Comp.IncidentType] += 1;

        var ev = new IncidentDisplayKillModifiedEvent(ent.Comp.IncidentType, 1, KillCount[ent.Comp.IncidentType]);
        RaiseLocalEvent(ent, ev, true);
    }
}
