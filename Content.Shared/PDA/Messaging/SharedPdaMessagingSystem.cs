using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Recipients;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Collections.Frozen;
using System.Text;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    private FrozenDictionary<ProtoId<PdaChatProfilePicturePrototype>, PdaChatProfilePicturePrototype> _profilePictures = default!;
    private readonly Dictionary<string, List<int>> _messageableIdsCounters = [];

    protected EntityQuery<PdaMessagingClientComponent> ClientQuery;
    protected EntityQuery<PdaMessagingServerComponent> ServerQuery;
    protected EntityQuery<PdaMessagingHistoryComponent> HistoryQuery;

    public override void Initialize()
    {
        base.Initialize();

        InitializeClient();
        InitializeServer();
        InitializeHistory();

        ClientQuery = GetEntityQuery<PdaMessagingClientComponent>();
        ServerQuery = GetEntityQuery<PdaMessagingServerComponent>();
        HistoryQuery = GetEntityQuery<PdaMessagingHistoryComponent>();

        UpdateCachedProfilePictures();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<PdaChatProfilePicturePrototype>())
            UpdateCachedProfilePictures();
    }

    private void UpdateCachedProfilePictures()
    {
        var protos = _prototype.GetInstances<PdaChatProfilePicturePrototype>();
        Dictionary<ProtoId<PdaChatProfilePicturePrototype>, PdaChatProfilePicturePrototype> newDict = [];
        newDict.EnsureCapacity(protos.Count);
        foreach (var (id, proto) in protos)
            newDict[id] = proto;

        _profilePictures = newDict.ToFrozenDictionary();
    }

    private PdaChatRecipientProfile GetDefaultProfile(Entity<PdaMessagingClientComponent> ent)
    {
        var (id, proto) = _random.Pick(_profilePictures);
        var name = proto.Name; // TODO: fetch the pda owners name via an event
        var profile = new PdaChatRecipientProfile()
        {
            Name = name,
            Picture = id
        };

        profile.Id = CreateMessageableId(profile.Prefix());
        return profile;
    }

    private string CreateMessageableId(string prefix)
    {
        if (!_messageableIdsCounters.TryGetValue(prefix, out var segments))
        {
            segments = [];
            segments.Add(0);
            _messageableIdsCounters[prefix] = segments;
        }

        // increment last segment
        var i = segments.Count - 1;
        segments[i]++;

        // handle overflow
        while (i >= 0 && segments[i] > BasePdaChatMessageable.SegmentMax)
        {
            segments[i] = 0;
            if (i == 0)
                segments.Insert(0, 1); // expand with new most significant segment
            else
                segments[i - 1]++;
            i--;
        }

        var length = prefix.Length + segments.Count * (BasePdaChatMessageable.IntegersPerSegment + 1);
        var id = new StringBuilder(length);
        id.Append(prefix);

        foreach (var segment in segments)
            id.Append($"-{segment.ToString($"D{BasePdaChatMessageable.IntegersPerSegment}")}");

        return id.ToString();
    }

    public FrozenDictionary<ProtoId<PdaChatProfilePicturePrototype>, PdaChatProfilePicturePrototype> GetSelectableProfilePictures() => _profilePictures;
}
