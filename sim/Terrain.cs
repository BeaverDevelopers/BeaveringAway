using Godot;
using System;
using System.Diagnostics;

public enum WaterDirection : byte
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

    public WaterDirection WaterDirection;

    // How much water is not allowed to move?
    public byte WaterLocked;
    public byte WaterHeight;
    public byte GroundHeight;
    public byte ObstructionHeight;
    public byte ObstructionHealth;
    public sbyte DangerLevel;

    public int TotalHeight()
    {
        return (int)GroundHeight * 5 + ObstructionHeight * 10 + WaterHeight;
    }
}

public struct GrassTile
{
    public int NextWateringCheck;
    public int HumidFor;
    public byte NextTileToCheck;
}

public struct WaterRandomTickData
{
    public int DistanceToSea;
    public int NextSoak;
}


public struct WaterFlow
{
    public int FlowX;
    public int FlowY;
    public float SmoothedFlowX;
    public float SmoothedFlowY;
}

public class Terrain
{
    public int Rows;
    public int Columns;
    public TerrainTile[,] Tiles;
    public WaterFlow[,] WaterTiles;
    public WaterRandomTickData[,] WaterRandomTickData;
    public Vector2I[] ProcessingOrder;
    public GrassTile[,] GrassTiles;

    public const byte NORMAL_TILE_HEIGHT = 2;
    public const byte MUDFLOOR_TILE_HEIGHT = 1;
    WaterRandomTickData BAD_WATER_RANDOM_TICK_DATA;

    private byte atlasCoordsToHeight(Vector2I vec)
    {
        if (vec.X <= -1)
        {
            return MUDFLOOR_TILE_HEIGHT;
        }
        return NORMAL_TILE_HEIGHT;
    }

    private bool atlasCoordsIsSeaTile(Vector2I vec) {
        return vec.X >= 0 && vec.X >= 0;
        //return (vec.X >= 13 && vec.Y <= 18 && vec.X >= 17 && vec.Y < 21);
    }

    public WaterRandomTickData GetWaterRandomTickData(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Columns || y >= Rows)
        {
            return BAD_WATER_RANDOM_TICK_DATA;
        }

        return WaterRandomTickData[x, y];
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

    public void DamageTile(int x, int y, int amount)
    {
        if (x < 0 || y < 0 || x >= Columns || y >= Rows)
        {
            // Can't be OOB.
            return;
        }

        int health = Tiles[x, y].ObstructionHealth;
        health -= amount;
        health = Math.Clamp(health, 0, 255);
        Tiles[x, y].ObstructionHealth = (byte)health;

        if (health == 0)
        {
            Tiles[x, y].ObstructionHeight = 0;
        }
    }

    public void MoveWater(int fromX, int fromY, int toX, int toY, WaterDirection velocity, bool allowEmpty)
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
        Tiles[fromX, fromY].WaterHeight = (byte)Math.Clamp(Tiles[fromX, fromY].WaterHeight - 1, 0, 255);
        Tiles[toX, toY].WaterHeight = (byte)Math.Clamp((int)1 + Tiles[toX, toY].WaterHeight, 0, 255);
        Tiles[toX, toY].WaterLocked = (byte)Math.Clamp((int)1 + Tiles[toX, toY].WaterHeight, 0, 255);
        if (Tiles[toX, toY].ObstructionHeight == 0 && Tiles[toX, toY].DangerLevel > -100 && Tiles[toX, toY].DangerLevel < 100)
        {
            // Hopefully the compiler is smart enough to unroll here.
            for (int i = 0; i < 5; i++)
            {
                if (Tiles[fromX, fromY].DangerLevel > Tiles[toX, toY].DangerLevel)
                {
                    Tiles[toX, toY].DangerLevel += 1;
                }
            }

            if (Tiles[fromX, fromY].DangerLevel < Tiles[toX, toY].DangerLevel)
            {
                Tiles[toX, toY].DangerLevel -= 1;
            }
            //Tiles[toX, toY].DangerLevel = Tiles[fromX, fromY].DangerLevel;
        }


        // Accumulate flow: +1 in the direction water moved
        int dx = toX - fromX;
        int dy = toY - fromY;
        WaterTiles[fromX, fromY].FlowX += dx;
        WaterTiles[fromX, fromY].FlowY += dy;
    }

    public void LoadTerrain(Node fromTerrain)
    {
        BAD_WATER_RANDOM_TICK_DATA.DistanceToSea = int.MaxValue;

        var terrain = fromTerrain.GetNode<TileMapLayer>("Level_0/Terrain");
        var sea = fromTerrain.GetNode<TileMapLayer>("Level_0/Sea");
        var clay = fromTerrain.GetNode<TileMapLayer>("Level_0/Clay");
        Debug.Assert(clay != null);

        var mapBounds = clay.GetUsedRect();
        var rows = mapBounds.Size.Y;
        var columns = mapBounds.Size.X;
        Debug.WriteLine($"Creating simulation tiles of size: {rows}, {columns}");
        Columns = columns;
        Rows = rows;
        Tiles = new TerrainTile[columns, rows];
        WaterTiles = new WaterFlow[columns, rows];
        GrassTiles = new GrassTile[columns, rows];
        WaterRandomTickData = new WaterRandomTickData[columns, rows];
        ProcessingOrder = new Vector2I[columns * rows];

        //var groundOverlay = fromTerrain.GetNode<TileMapLayer>("Level_0/Ground/Ground1");
        //Debug.Assert(groundOverlay != null);

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                int i = x + y * Rows;
                var coord = new Vector2I(x, y);
                ProcessingOrder[i] = coord;
                
                var atlasCoords = terrain.GetCellAtlasCoords(coord);
                byte height = atlasCoordsToHeight(atlasCoords);
                Tiles[x, y].GroundHeight = height;
                WaterRandomTickData[x, y].DistanceToSea = atlasCoordsIsSeaTile(sea.GetCellAtlasCoords(coord)) ? 0 : (height == MUDFLOOR_TILE_HEIGHT ? int.MaxValue / 2 : int.MaxValue);
                WaterRandomTickData[x, y].NextSoak = Random.Shared.Next(0, 1600);
                GrassTiles[x, y].NextWateringCheck = Random.Shared.Next(0, 1600);
                GrassTiles[x, y].NextTileToCheck = (byte)Random.Shared.Next(0, 255);
            }
        }
        Random.Shared.Shuffle(ProcessingOrder);
    }
}
