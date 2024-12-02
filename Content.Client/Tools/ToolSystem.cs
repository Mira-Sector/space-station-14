using Content.Client.Items;
using Content.Client.Tools.Components;
using Content.Client.Tools.UI;
using Content.Shared.Tools.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Spawners;
using System.Numerics;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Client.Tools
{
    public sealed class ToolSystem : SharedToolSystem
    {
        [Dependency] private readonly AnimationPlayerSystem _anim = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        private readonly string AnimationKey = "tool_interact";

        public override void Initialize()
        {
            base.Initialize();

            Subs.ItemStatus<WelderComponent>(ent => new WelderStatusControl(ent, EntityManager, this));
            Subs.ItemStatus<MultipleToolComponent>(ent => new MultipleToolStatusControl(ent));

            SubscribeLocalEvent<ToolComponent, ToolUseAttemptEvent>(OnToolAttempt);
            SubscribeLocalEvent<ToolComponent, TileToolDoAfterEvent>(OnToolTile);

            SubscribeLocalEvent<ToolAnimationComponent, TimedDespawnEvent>(OnAnimationComplete);
        }

        private void OnToolAttempt(EntityUid uid, ToolComponent component, ToolUseAttemptEvent args)
        {
            if (args.Cancelled || args.Target is not {} target || uid == args.Target)
                return;

            DoAnimation(uid, args.User, Transform(target).LocalPosition, component);
        }

        private void OnToolTile(EntityUid uid, ToolComponent component, TileToolDoAfterEvent args)
        {
            if (args.Cancelled)
                return;

            DoAnimation(uid, args.User, args.GridTile, component);
        }

        private void DoAnimation(EntityUid uid, EntityUid user, Vector2 targetPos, ToolComponent component)
        {
            if (component.HasAnimation)
                return;

            var metadata = MetaData(uid);

            if (IsPaused(uid, metadata))
                return;

            var userPos = Transform(user).LocalPosition;
            var initialAngle = Transform(user).LocalRotation;

            var animatableClone = Spawn("clientsideclone", Transform(user).Coordinates);
            var val = metadata.EntityName;
            _metaData.SetEntityName(animatableClone, val);

            if (!TryComp(uid, out SpriteComponent? sprite0))
            {
                Log.Error("Entity ({0}) couldn't be animated for pickup since it doesn't have a {1}!", metadata.EntityName, nameof(SpriteComponent));
                return;
            }

            var sprite = Comp<SpriteComponent>(animatableClone);
            sprite.CopyFrom(sprite0);
            sprite.Visible = true;

            var player = Comp<AnimationPlayerComponent>(animatableClone);

            var despawn = EnsureComp<TimedDespawnComponent>(animatableClone);
            despawn.Lifetime = 0.5f;
            EnsureComp<ToolAnimationComponent>(animatableClone).AnimationSpawner = uid;

            _transform.SetLocalRotationNoLerp(animatableClone, initialAngle);

            var startingColor = sprite0.Color;
            var endingColor = sprite0.Color.WithAlpha(0f);

            var animation = new Animation()
            {
                Length = TimeSpan.FromSeconds(0.5),
                AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Scale),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(sprite0.Scale * 1.125f, 0.0f),
                            new AnimationTrackProperty.KeyFrame(sprite0.Scale * 1.5f, 0.5f)
                        }
                    },

                    new AnimationTrackComponentProperty
                    {
                        ComponentType = typeof(TransformComponent),
                        Property = nameof(TransformComponent.LocalPosition),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(userPos, 0),
                            new AnimationTrackProperty.KeyFrame(targetPos, 0.5f)
                        }
                    },

                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Color),
                        InterpolationMode = AnimationInterpolationMode.Linear,
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(startingColor, 0.0f),
                            new AnimationTrackProperty.KeyFrame(endingColor, 0.5f)
                        }
                    }
                }
            };

            _anim.Play((animatableClone, player), animation, AnimationKey);
            component.HasAnimation = true;
        }

        private void OnAnimationComplete(EntityUid uid, ToolAnimationComponent component, ref TimedDespawnEvent args)
        {
            if (!TryComp<ToolComponent>(component.AnimationSpawner, out var toolComp))
                return;

            toolComp.HasAnimation = false;
        }

        public override void SetMultipleTool(EntityUid uid,
        MultipleToolComponent? multiple = null,
        ToolComponent? tool = null,
        bool playSound = false,
        EntityUid? user = null)
        {
            if (!Resolve(uid, ref multiple))
                return;

            base.SetMultipleTool(uid, multiple, tool, playSound, user);
            multiple.UiUpdateNeeded = true;

            // TODO replace this with appearance + visualizer
            // in order to convert this to a specifier, the manner in which the sprite is specified in yaml needs to be updated.

            if (multiple.Entries.Length > multiple.CurrentEntry && TryComp(uid, out SpriteComponent? sprite))
            {
                var current = multiple.Entries[multiple.CurrentEntry];
                if (current.Sprite != null)
                    sprite.LayerSetSprite(0, current.Sprite);
            }
        }
    }
}
