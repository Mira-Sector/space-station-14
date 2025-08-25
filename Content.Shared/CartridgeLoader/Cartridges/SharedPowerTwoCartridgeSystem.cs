using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Shared.CartridgeLoader.Cartridges;

public abstract partial class SharedPowerTwoCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PowerTwoCartridgeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PowerTwoCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<PowerTwoCartridgeComponent, CartridgeActivatedEvent>(OnUiActivated);
        SubscribeLocalEvent<PowerTwoCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnInit(Entity<PowerTwoCartridgeComponent> ent, ref ComponentInit args)
    {
        NewGame(ent);
    }

    private void OnUiMessage(Entity<PowerTwoCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is PowerTwoUiMoveMessageEvent movement)
        {
            if (ent.Comp.GameState != PowerTwoGameState.InGame)
                return;

            Move(ent, movement.Direction);
            UpdateUi(ent, GetEntity(args.LoaderUid));
        }
        else if (args is PowerTwoUiNewGameMessageEvent)
        {
            NewGame(ent);
            UpdateUi(ent, GetEntity(args.LoaderUid));
        }
    }

    private void OnUiActivated(Entity<PowerTwoCartridgeComponent> ent, ref CartridgeActivatedEvent args)
    {
        NewGame(ent);
    }

    private void OnUiReady(Entity<PowerTwoCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader);
    }

    protected virtual void UpdateUi(Entity<PowerTwoCartridgeComponent> ent, EntityUid loader)
    {
    }

    protected void NewGame(Entity<PowerTwoCartridgeComponent> ent)
    {
        ent.Comp.GameState = PowerTwoGameState.InGame;
        ent.Comp.StartTime = _timing.CurTime;

        ent.Comp.Grid = new int?[ConvertToFlattenedIndex(ent.Comp.GridSize, ent.Comp.GridSize)];

        // cant be null as we just made a new grid
        var newCoords = NewUnblockedPieceCoords(ent)!.Value;
        var newScore = GetStartingValue(ent);
        var newIndex = ConvertToFlattenedIndex(newCoords, ent.Comp.GridSize);
        ent.Comp.Grid[newIndex] = newScore;
        Dirty(ent);
    }

    protected void Move(Entity<PowerTwoCartridgeComponent> ent, PowerTwoDirection dir)
    {
        var moved = false;

        var axis = dir.GetDirectionAxis();
        var flip = dir.ShouldFlip();

        var lineCount = axis == PowerTwoDirectionAxis.Horizontal ? ent.Comp.GridSize.Y : ent.Comp.GridSize.X;

        for (var i = 0; i < lineCount; i++)
        {
            var line = ExtractLine(ent.Comp.Grid, ent.Comp.GridSize, i, axis);

            if (flip)
                Array.Reverse(line);

            var mergedLine = MergeLine(line);

            if (flip)
                Array.Reverse(mergedLine);

            moved |= !line.SequenceEqual(mergedLine);
            InsertLine(ent.Comp.Grid, ent.Comp.GridSize, i, axis, mergedLine);
        }

        if (!moved)
            return;

        if (ent.Comp.Grid.Any(cell => cell >= ent.Comp.WinningScore))
        {
            ent.Comp.GameState = PowerTwoGameState.Won;
            Dirty(ent);
            return;
        }

        if (NewUnblockedPieceCoords(ent) is { } newCoords)
        {
            var newValue = GetStartingValue(ent);
            var newIndex = ConvertToFlattenedIndex(newCoords, ent.Comp.GridSize);
            ent.Comp.Grid[newIndex] = newValue;
            Dirty(ent);
            return;
        }

        if (!HasValidMoves(ent))
            ent.Comp.GameState = PowerTwoGameState.GameOver;

        Dirty(ent);
    }

    private static int?[] ExtractLine(int?[] grid, Vector2i gridSize, int index, PowerTwoDirectionAxis axis)
    {
        var line = new int?[axis == PowerTwoDirectionAxis.Horizontal ? gridSize.X : gridSize.Y];

        switch (axis)
        {
            case PowerTwoDirectionAxis.Horizontal:
                for (var x = 0; x < gridSize.X; x++)
                {
                    var gridIndex = ConvertToFlattenedIndex(new(x, index), gridSize);
                    line[x] = grid[gridIndex];
                }

                break;
            case PowerTwoDirectionAxis.Vertical:
                for (var y = 0; y < gridSize.Y; y++)
                {
                    var gridIndex = ConvertToFlattenedIndex(new(index, y), gridSize);
                    line[y] = grid[gridIndex];
                }

                break;
        }

        return line;
    }

    private static void InsertLine(int?[] grid, Vector2i gridSize, int index, PowerTwoDirectionAxis axis, int?[] line)
    {
        switch (axis)
        {
            case PowerTwoDirectionAxis.Horizontal:
                for (var x = 0; x < gridSize.X; x++)
                {
                    var gridIndex = ConvertToFlattenedIndex(new(x, index), gridSize);
                    grid[gridIndex] = line[x];
                }

                break;
            case PowerTwoDirectionAxis.Vertical:
                for (var y = 0; y < gridSize.Y; y++)
                {
                    var gridIndex = ConvertToFlattenedIndex(new(index, y), gridSize);
                    grid[gridIndex] = line[y];
                }

                break;
        }
    }

    private static int?[] MergeLine(int?[] line)
    {
        var length = line.Length;
        var merged = new int?[length];

        // compact non null values
        var compact = new int[length];
        var compactCount = 0;

        for (var i = 0; i < length; i++)
        {
            if (line[i] is { } value)
            {
                compact[compactCount] = value;
                compactCount++;
            }
        }

        // merge adjacent equals
        var writeIndex = 0;
        for (var i = 0; i < compactCount; i++)
        {
            // is this the last element and is the current tile equal to the next tile?
            if (i < compactCount - 1 && compact[i] == compact[i + 1])
            {
                // combine
                merged[writeIndex] = compact[i] * 2;
                writeIndex++;
                i++; // skip the next since it was merged with this one
            }
            else
            {
                merged[writeIndex] = compact[i];
                writeIndex++;
            }
        }

        // fill remaining with null
        for (var i = writeIndex; i < length; i++)
            merged[i] = null;

        return merged;
    }

    private Vector2i? NewUnblockedPieceCoords(Entity<PowerTwoCartridgeComponent> ent)
    {
        var unblocked = UnblockedPositions(ent).ToArray();
        if (!unblocked.Any())
            return null;

        return _random.Pick(unblocked);
    }

    private int GetStartingValue(Entity<PowerTwoCartridgeComponent> ent)
    {
        return _random.Pick(ent.Comp.StartingScores);
    }

    private static IEnumerable<Vector2i> UnblockedPositions(Entity<PowerTwoCartridgeComponent> ent)
    {
        for (var x = 0; x < ent.Comp.GridSize.X; x++)
        {
            for (var y = 0; y < ent.Comp.GridSize.Y; y++)
            {
                var index = ConvertToFlattenedIndex(new(x, y), ent.Comp.GridSize);
                if (ent.Comp.Grid[index] == null)
                    yield return new(x, y);
            }
        }
    }

    private static bool HasValidMoves(Entity<PowerTwoCartridgeComponent> ent)
    {
        for (var x = 0; x < ent.Comp.GridSize.X; x++)
        {
            for (var y = 0; y < ent.Comp.GridSize.Y; y++)
            {
                var index = ConvertToFlattenedIndex(new Vector2i(x, y), ent.Comp.GridSize);
                var value = ent.Comp.Grid[index];
                if (value == null)
                    return true; // empty space exists, move possible

                // check neighbors for merge possibility
                var neighbors = new[]
                {
                    new Vector2i(x + 1, y),
                    new Vector2i(x, y + 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.X < ent.Comp.GridSize.X && n.Y < ent.Comp.GridSize.Y)
                    {
                        var neighborIndex = ConvertToFlattenedIndex(n, ent.Comp.GridSize);
                        if (ent.Comp.Grid[neighborIndex] == value)
                            return true; // merge possible
                    }
                }
            }
        }

        // no moves left
        return false;
    }

    // no serializing multi-dim arrays so this is needed
    public static int ConvertToFlattenedIndex(Vector2i pos, Vector2i gridSize)
    {
        return pos.X + pos.Y * gridSize.X;
    }

    public static Vector2i ConvertFromFlattenedIndex(int index, Vector2i gridSize)
    {
        var x = index % gridSize.X;
        var y = index / gridSize.X;
        return new Vector2i(x, y);
    }
}
