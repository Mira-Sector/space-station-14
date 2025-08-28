using System.Collections.Frozen;
using Content.Shared.PDA.Messaging.Components;
using Content.Shared.PDA.Messaging.Recipients;
using Content.Shared.Station;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.PDA.Messaging;

public abstract partial class SharedPdaMessagingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    private FrozenDictionary<ProtoId<PdaChatProfilePicturePrototype>, PdaChatProfilePicturePrototype> _profilePictures = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeClient();
        InitializeServer();
        InitializeHistory();

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
        return new()
        {
            Name = name,
            Picture = id
        };
    }
}
