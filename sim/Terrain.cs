using Godot;
using System;
using System.Diagnostics;

public enum WaterVelocity : byte
{
    None,
    Down,
    Up,
    Left,
    Right
}

[DebuggerDisplay("Height: {GroundHeight}, Water: {WaterHeight}")]
public struct TerrainTile
{
    public byte WaterHeight;
    public WaterVelocity WaterVelocity;
    public byte GroundHeight;
    public byte ObstructionHeight;
    public bool Locked;
    public byte WaterShown;

    public int TotalHeight()
    {
        return (int)GroundHeight * 5 + ObstructionHeight * 2 + WaterHeight;
    }
}

public class Terrain
{
    public int Rows;
    public int Columns;
    public TerrainTile[,] Tiles;

    const int NORMAL_CLIFF_HEIGHT = 1;

    private int atlasCoordsToHeightChange(Vector2I vec) {
        if (vec.Y == 11 && vec.X >= 8 && vec.X <= 10)
        {
            return NORMAL_CLIFF_HEIGHT;
        }
        if (vec.Y == 12 && vec.X == 7)
        {
            return NORMAL_CLIFF_HEIGHT;
        }
        if (vec.Y == 12 && vec.X == 11)
        {
            return NORMAL_CLIFF_HEIGHT;
        }
        if (vec.Y == 15 && vec.X >= 8 && vec.X <= 10)
        {
            return -NORMAL_CLIFF_HEIGHT;
        }
        if (vec.Y == 14 && vec.X == 8)
        {
            return -NORMAL_CLIFF_HEIGHT;
        }
        if (vec.Y == 14 && vec.X == 11)
        {
            return -NORMAL_CLIFF_HEIGHT;
        }
        return 0;
    }

    private void calculateGroundHeightForColumn(int column, int rows, TileMapLayer from) {
        byte height = 128;
        for (int i = 0; i < rows; i++)
        {
            var atlasCoords = from.GetCellAtlasCoords(new Vector2I(column, i));
            height = (byte)(atlasCoordsToHeightChange(atlasCoords) + height);

            Tiles[column, i].GroundHeight = height;
            Tiles[column, i].WaterVelocity = WaterVelocity.Down;
        }
    }
    public int TileHeight(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Columns || y >= Rows)
        {
            return 512;
        }

        return Tiles[x, y].TotalHeight();
    }

    public int TileWater(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Columns || y >= Rows)
        {
            return 0;
        }

        return Tiles[x, y].WaterHeight;
    }

    public void MoveWater(int fromX, int fromY, int toX, int toY, WaterVelocity velocity, bool allowEmpty)
    {
        if (toX < 0 || toY < 0 || toX >= Columns || toY >= Rows)
        {
            // Can't be OOB.
            return;
        }

        var fromHeight = TileHeight(fromX, fromY);
        var toHeight = TileHeight(toX, toY);

        if (fromHeight <= toHeight)
        {
            // There must be a hight difference for water to move.
            return;
        }

        if (Tiles[fromX, fromY].WaterHeight == 0)
        {
            // We need at least some water to move.
            return;
        }

        if (fromHeight == toHeight + 1 && !allowEmpty)
        {
            // If we don't return here then we won't get pools.
            // Idea is to not do moves that result in a tile becoming completely empty.
            return;
        }

        // TODO: Perform saturated add and remove using bitwise operations instead.
        Tiles[fromX, fromY].WaterHeight = (byte)Math.Max(0, Math.Min(255, Tiles[fromX, fromY].WaterHeight - 1));
        Tiles[toX, toY].WaterHeight = (byte)Math.Max(0, Math.Min(255, (int)1 + Tiles[toX, toY].WaterHeight));
        Tiles[toX, toY].WaterVelocity = velocity;
        Tiles[toX, toY].Locked = true;
        Tiles[toX, toY].WaterShown = 20;
    }

    public void LoadTerrain(Node fromTerrain)
    {
        var bottomGround = fromTerrain.GetNode<TileMapLayer>("Level_0/Ground");
        Debug.Assert(bottomGround != null);

        var mapBounds = bottomGround.GetUsedRect();
        var rows = mapBounds.Size.Y;
        var columns = mapBounds.Size.X;
        Debug.WriteLine($"Creating simulation tiles of size: {rows}, {columns}");
        Columns = columns;
        Rows = rows;
        Tiles = new TerrainTile[columns, rows];

        var groundOverlay = fromTerrain.GetNode<TileMapLayer>("Level_0/Ground/Ground1");
        Debug.Assert(groundOverlay != null);

        for (int i = 0; i < columns; i++)
        {
            calculateGroundHeightForColumn(i, rows, groundOverlay);
        }
    }
}
