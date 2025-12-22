using Content.Client.PolygonRenderer;
using Content.Shared.Arcade.Racer;
using Content.Shared.Arcade.Racer.Components;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.Arcade.Racer;

public sealed partial class RacerGameViewportControl : PolygonRendererControl
{
    private void DrawDebug(DrawingHandleScreen handle, RacerGameState state, Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
    {
        var flags = _racer.GetDebugFlags();

        if (flags.HasFlag(RacerArcadeDebugFlags.ControlledData))
            DrawDebugControlledData(handle, state, cabinet, viewer);
    }

    private void DrawDebugControlledData(DrawingHandleScreen handle, RacerGameState state, Entity<RacerArcadeComponent> cabinet, EntityUid viewer)
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
