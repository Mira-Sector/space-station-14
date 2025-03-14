using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Ghost;

public sealed class GhostVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostVisualsComponent, GhostedEvent>(OnGhosted);
    }

    private void OnGhosted(Entity<GhostVisualsComponent> ent, ref GhostedEvent args)
    {
        // we use the last body they were in first
        // if that fails we then use what they selected in the lobby
        var lastBody = args.Mind.Comp.CurrentEntity;

        if (!TryComp<HumanoidAppearanceComponent>(lastBody, out var humanoidComp))
            return;

        var usingProfile = humanoidComp == null;

        if (humanoidComp == null)
        {
            if (args.Mind.Comp.Session is not {} session)
                return;

            var profile = _ticker.GetPlayerProfile(session);

            if (!_prototype.TryIndex(profile.Species, out var species))
                return;

            lastBody = EntityManager.Spawn(species.Prototype);
            _humanoid.LoadProfile(lastBody.Value, profile);

            if (!TryComp<HumanoidAppearanceComponent>(lastBody, out humanoidComp))
                return;
        }

        if (ent.Comp.TransferColor)
            _appearance.SetData(ent.Owner, GhostVisuals.Color, humanoidComp.SkinColor);

        foreach (var layerId in ent.Comp.LayersToTransfer.Except(humanoidComp.HiddenLayers))
        {
            if (!humanoidComp.BaseLayers.TryGetValue(layerId, out var layer))
                continue;

            if (layer.BaseSprite == null)
                continue;

            _appearance.SetData(ent.Owner, layerId, layer.BaseSprite);
        }

        foreach (var markingCategory in ent.Comp.MarkingsToTransfer)
        {
            if (!humanoidComp.MarkingSet.Markings.TryGetValue(markingCategory, out var markings))
                continue;

            foreach (var marking in markings)
            {
                if (!marking.Visible)
                    continue;

                if (!_prototype.TryIndex<MarkingPrototype>(marking.MarkingId, out var markingProto))
                    continue;

                ent.Comp.Markings.Add(markingProto.BodyPart);
                _appearance.SetData(ent.Owner, markingProto.BodyPart, markingProto.Sprites.First());
            }
        }

        Dirty(ent);

        if (usingProfile)
            EntityManager.DeleteEntity(lastBody.Value);
    }
}
