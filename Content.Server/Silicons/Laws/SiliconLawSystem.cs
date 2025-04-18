using System.Linq;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Radio.Components;
using Content.Server.Roles;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Audio;

namespace Content.Server.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconLawBoundComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SiliconLawBoundComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SiliconLawBoundComponent, ToggleLawsScreenEvent>(OnToggleLawsScreen);
        SubscribeLocalEvent<SiliconLawBoundComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<SiliconLawBoundComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<SiliconLawProviderComponent, GetSiliconLawsEvent>(OnDirectedGetLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, IonStormLawsEvent>(OnIonStormLaws);
        SubscribeLocalEvent<SiliconLawProviderComponent, MindAddedMessage>(OnLawProviderMindAdded);
        SubscribeLocalEvent<SiliconLawProviderComponent, MindRemovedMessage>(OnLawProviderMindRemoved);
        SubscribeLocalEvent<SiliconLawProviderComponent, SiliconEmaggedEvent>(OnEmagLawsAdded);

        SubscribeNetworkEvent<SiliconGetLawsEvent>(OnGetLaws);
    }

    private void OnMapInit(EntityUid uid, SiliconLawBoundComponent component, MapInitEvent args)
    {
        GetLaws(uid, component);
    }

    private void OnMindAdded(EntityUid uid, SiliconLawBoundComponent component, MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.FromHex("#5ed7aa"));

        if (!TryComp<SiliconLawProviderComponent>(uid, out var lawcomp))
            return;

        if (!lawcomp.Subverted)
            return;

        var modifedLawMsg = Loc.GetString("laws-notify-subverted");
        var modifiedLawWrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", modifedLawMsg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, modifedLawMsg, modifiedLawWrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);
    }

    private void OnLawProviderMindAdded(Entity<SiliconLawProviderComponent> ent, ref MindAddedMessage args)
    {
        if (!ent.Comp.Subverted || !ent.Comp.SubvertedMind)
            return;

        EnsureSubvertedSiliconRole(args.Mind);
    }

    private void OnLawProviderMindRemoved(Entity<SiliconLawProviderComponent> ent, ref MindRemovedMessage args)
    {
        if (!ent.Comp.Subverted || !ent.Comp.SubvertedMind)
            return;
        RemoveSubvertedSiliconRole(args.Mind);
    }

    private void OnToggleLawsScreen(EntityUid uid, SiliconLawBoundComponent component, ToggleLawsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, SiliconLawsUiKey.Key, actor.PlayerSession);
    }

    private void OnBoundUIOpened(EntityUid uid, SiliconLawBoundComponent component, BoundUIOpenedEvent args)
    {
        TryComp(uid, out IntrinsicRadioTransmitterComponent? intrinsicRadio);
        var radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(uid).Laws, radioChannels);
        _userInterface.SetUiState(args.Entity, SiliconLawsUiKey.Key, state);
    }

    private void OnPlayerSpawnComplete(EntityUid uid, SiliconLawBoundComponent component, PlayerSpawnCompleteEvent args)
    {
        component.LastLawProvider = args.Station;
    }

    private void OnDirectedGetLaws(EntityUid uid, SiliconLawProviderComponent component, ref GetSiliconLawsEvent args)
    {
        if (args.Handled)
            return;

        if (component.Lawset == null)
            component.Lawset = GetLawset(component.Laws);

        args.Laws = component.Lawset;
        args.Handled = true;
    }

    private void OnIonStormLaws(EntityUid uid, SiliconLawProviderComponent component, ref IonStormLawsEvent args)
    {
        // Emagged borgs are immune to ion storm
        if (!_emag.CheckFlag(uid, EmagType.Interaction))
        {
            SetLaws(args.Lawset.Laws, uid);

            // Show the silicon has been subverted.
            component.Subverted = true;

            // new laws may allow antagonist behaviour so make it clear for admins
            if(_mind.TryGetMind(uid, out var mindId, out _))
                EnsureSubvertedSiliconRole(mindId);

        }
    }

    private void OnEmagLawsAdded(EntityUid uid, SiliconLawProviderComponent component, ref SiliconEmaggedEvent args)
    {
        var lawset = component.Lawset ?? GetLawset(component.Laws);

        // Show the silicon has been subverted.
        component.Subverted = true;

        // Add the first emag law before the others
        lawset.Laws.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-custom", ("name", Name(args.user)), ("title", Loc.GetString(lawset.ObeysTo))),
            Order = 0
        });

        //Add the secrecy law after the others
        lawset.Laws.Add(new SiliconLaw
        {
            LawString = Loc.GetString("law-emag-secrecy", ("faction", Loc.GetString(lawset.ObeysTo))),
            Order = lawset.Laws.Max(law => law.Order) + 1
        });

        SetLaws(lawset.Laws, uid);
    }

    protected override void EnsureSubvertedSiliconRole(EntityUid mindId)
    {
        base.EnsureSubvertedSiliconRole(mindId);

        if (!_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindAddRole(mindId, "MindRoleSubvertedSilicon", silent: true);
    }

    protected override void RemoveSubvertedSiliconRole(EntityUid mindId)
    {
        base.RemoveSubvertedSiliconRole(mindId);

        if (_roles.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            _roles.MindTryRemoveRole<SubvertedSiliconRoleComponent>(mindId);
    }

    private void OnGetLaws(SiliconGetLawsEvent args)
    {
        args.Laws = GetLaws(GetEntity(args.Target));
    }

    public override SiliconLawset GetLaws(EntityUid uid, SiliconLawBoundComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new SiliconLawset();

        var ev = new GetSiliconLawsEvent(uid);

        if (_mind.TryGetMind(uid, out var _, out var mind))
        {
            foreach (var role in mind.MindRoles)
            {
                RaiseLocalEvent(role, ref ev);

                if (ev.Handled)
                    break;
            }
        }

        ev.Handled = false;

        RaiseLocalEvent(uid, ref ev);

        var laws = ev.Laws;

        foreach (var priorityLaw in ev.PriorityLaws)
        {
            laws.Laws.Add(priorityLaw);
        }

        if (ev.Handled)
        {
            component.LastLawProvider = uid;
            return laws;
        }

        var xform = Transform(uid);

        if (_station.GetOwningStation(uid, xform) is { } station)
        {
            RaiseLocalEvent(station, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = station;
                return laws;
            }
        }

        if (xform.GridUid is { } grid)
        {
            RaiseLocalEvent(grid, ref ev);
            if (ev.Handled)
            {
                component.LastLawProvider = grid;
                return laws;
            }
        }

        if (component.LastLawProvider == null ||
            Deleted(component.LastLawProvider) ||
            Terminating(component.LastLawProvider.Value))
        {
            component.LastLawProvider = null;
        }
        else
        {
            RaiseLocalEvent(component.LastLawProvider.Value, ref ev);
            if (ev.Handled)
            {
                return laws;
            }
        }

        RaiseLocalEvent(ref ev);

        laws = ev.Laws;

        foreach (var priorityLaw in ev.PriorityLaws)
        {
            laws.Laws.Add(priorityLaw);
        }

        return laws;
    }

    public override void NotifyLawsChanged(EntityUid uid, SoundSpecifier? cue = null)
    {
        base.NotifyLawsChanged(uid, cue);

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        var msg = Loc.GetString("laws-update-notify");
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrappedMessage, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);

        if (cue != null && _mind.TryGetMind(uid, out var mindId, out _))
            _roles.MindPlaySound(mindId, cue);
    }

    /// <summary>
    /// Extract all the laws from a lawset's prototype ids.
    /// </summary>
    public SiliconLawset GetLawset(ProtoId<SiliconLawsetPrototype> lawset)
    {
        var proto = _prototype.Index(lawset);
        var laws = new SiliconLawset()
        {
            Laws = new List<SiliconLaw>(proto.Laws.Count)
        };
        foreach (var law in proto.Laws)
        {
            laws.Laws.Add(_prototype.Index<SiliconLawPrototype>(law));
        }
        laws.ObeysTo = proto.ObeysTo;

        return laws;
    }

    protected override void OnUpdaterInsert(Entity<SiliconLawUpdaterComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // TODO: Prediction dump this
        if (!TryComp(args.Entity, out SiliconLawProviderComponent? provider))
            return;

        var lawset = GetLawset(provider.Laws).Laws;
        var query = EntityManager.CompRegistryQueryEnumerator(ent.Comp.Components);

        while (query.MoveNext(out var update))
        {
            var newLaws = lawset;

            if (_mind.TryGetMind(update, out var _, out var mind))
            {
                var ev = new UpdatedSiliconLawsEvent(update);

                foreach (var role in mind.MindRoles)
                {
                    RaiseLocalEvent(role, ref ev);

                    if (ev.Handled)
                        break;
                }

                foreach (var law in ev.PriorityLaws)
                {
                    newLaws.Add(law);
                }
            }

            SetLaws(newLaws, update, provider.LawUploadSound);
        }
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed class LawsCommand : ToolshedCommand
{
    private SiliconLawSystem? _law;

    [CommandImplementation("list")]
    public IEnumerable<EntityUid> List()
    {
        var query = EntityManager.EntityQueryEnumerator<SiliconLawBoundComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            yield return uid;
        }
    }

    [CommandImplementation("get")]
    public IEnumerable<string> Get([PipedArgument] EntityUid lawbound)
    {
        _law ??= GetSys<SiliconLawSystem>();

        foreach (var law in _law.GetLaws(lawbound).Laws)
        {
            yield return $"law {law.LawIdentifierOverride ?? law.Order.ToString()}: {Loc.GetString(law.LawString)}";
        }
    }
}
