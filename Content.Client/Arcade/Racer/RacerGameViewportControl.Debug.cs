using Content.Client.PolygonRenderer;
using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Components;
using Content.Shared.Arcade.Racer.Stage;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerGameViewportControl : PolygonRendererControl
{
    private void DebugBefore(DrawingHandleScreen handle, RacerGameState state, Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
    {
        var flags = _racer.GetDebugFlags();

        if (flags.HasFlag(RacerArcadeDebugFlags.Collision))
            DrawCollisions(state);
    }

    private void DebugAfter(DrawingHandleScreen handle, RacerGameState state, Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
    {
        var flags = _racer.GetDebugFlags();

        if (flags.HasFlag(RacerArcadeDebugFlags.ControlledData))
            DrawDebugControlledData(handle, cabinet, viewer);
    }

    private void DrawCollisions(RacerGameState state)
    {
        var stage = _prototype.Index(state.CurrentStage);
        Models.EnsureCapacity(Models.Count + stage.Graph.CollisionShapes.Count);
        foreach (var entry in stage.Graph.CollisionShapes.Values)
        {
            var model = entry.Shape.GetDebugModel(_prototype);
            Models.Add(model);
        }

        foreach (var netObj in state.Objects)
        {
            if (!_entity.TryGetEntity(netObj, out var obj))
                continue;

            if (!_entity.TryGetComponent<RacerArcadeObjectCollisionComponent>(obj, out var objCollision))
                continue;

            var objData = _entity.GetComponent<RacerArcadeObjectComponent>(obj.Value);

            Models.EnsureCapacity(Models.Count + objCollision.Shapes.Count);

            foreach (var entry in objCollision.Shapes.Values)
            {
                var model = entry.Shape.GetDebugModel(_prototype);
                model.ModelMatrix *= Matrix4.Rotate(objData.Rotation) * Matrix4.CreateTranslation(objData.Position);
                Models.Add(model);
            }
        }
    }

    private void DrawDebugControlledData(DrawingHandleScreen handle, Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
    {
        var fontY = 0f;

        if (!_racer.TryGetControlledObject(cabinet!, viewer, out var controlled))
            return;

        var data = _entity.GetComponent<RacerArcadeObjectComponent>(controlled.Value.Owner);
        DrawText("Data:");
        DrawText($"Pos: {data.Position}");
        DrawText($"Rot: ({data.Rotation})");
        DrawText($"Prev Pos: ({data.PreviousPosition})");
        DrawText($"Prev Rot: ({data.PreviousRotation})");

        if (_entity.TryGetComponent<RacerArcadeObjectPhysicsComponent>(controlled.Value.Owner, out var physics))
        {
            NewLine();
            DrawText("Physics:");
            DrawText($"Accumulated Force: {physics.AccumulatedForce}");
            DrawText($"Accumulated Torque: {physics.AccumulatedTorque}");
            DrawText($"Velocity: {physics.Velocity}");
            DrawText($"Angular Velocity: {physics.AngularVelocity}");
        }

        void DrawText(string msg)
        {
            var pos = new Vector2(0f, fontY);
            handle.DrawString(_font, pos, msg);
            NewLine();
        }

        void NewLine()
        {
            fontY += _font.GetLineHeight(1f);
        }
    }

}
