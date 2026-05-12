using Godot;
using System;
using System.Diagnostics;

public class GrassSimulation
{
    static bool Y = true;
    static bool N = false;
    public static bool[,] Circle = {
        { N, N, N, Y, Y, Y, N, N, N },
        { N, N, Y, Y, Y, Y, Y, N, N },
        { N, Y, Y, Y, Y, Y, Y, Y, N },
        { Y, Y, Y, Y, Y, Y, Y, Y, Y },
        { Y, Y, Y, Y, Y, Y, Y, Y, Y },
        { Y, Y, Y, Y, Y, Y, Y, Y, Y },
        { N, Y, Y, Y, Y, Y, Y, Y, N },
        { N, N, Y, Y, Y, Y, Y, N, N },
        { N, N, N, Y, Y, Y, N, N, N },
    };

    public static Vector2I[] TileCoordinate = { };

    public static void Init()
    {
        var amount = 0;
        foreach (var v in Circle)
        {
            amount = v ? amount + 1 : amount;
        }

        TileCoordinate = new Vector2I[amount];
        var index = 0;

        var offsetY = (int)Math.Ceiling(((float)Circle.GetLength(0)) / 2);
        var offsetX = (int)Math.Ceiling(((float)Circle.GetLength(1)) / 2);

        for (int y = 0; y < Circle.GetLength(1); y++)
        {
            for (int x = 0; x < Circle.GetLength(0); x++)
            {
                bool value = Circle[x, y];
                if (value)
                {
                    TileCoordinate[index] = new Vector2I(x - offsetX, y - offsetY);
                    index = index + 1;
                }
            }
        }

        //Debugger.Launch();
    }

    public static int DEFAULT_HYDRATION = 160000;

    public static void HydrateRegion(Terrain terrain, int previousIndex, int x, int y, int strength)
    {
        for (int i = 0; i < 15; i++)
        {
            int index = (previousIndex + i) % TileCoordinate.Length;
            terrain.GrassTiles[x, y].NextTileToCheck = (byte)index;
            Vector2I which = new Vector2I(x, y) + TileCoordinate[index];
            if (which.X >= 0 && which.Y >= 0 && which.X < terrain.Columns && which.Y < terrain.Rows)
            {
                terrain.GrassTiles[x, y].HumidFor = Math.Max(terrain.GrassTiles[x, y].HumidFor, strength);
            }
        }
    }

    public static void Run(Terrain terrain, int tick)
    {
        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                terrain.GrassTiles[x, y].HumidFor = terrain.GrassTiles[x, y].HumidFor > 0 ? terrain.GrassTiles[x, y].HumidFor - 16 : 0;

                if (terrain.GrassTiles[x, y].HumidFor > 0)
                {
                    continue;
                }

                if (terrain.GrassTiles[x, y].NextWateringCheck > 0)
                {
                    terrain.GrassTiles[x, y].NextWateringCheck = terrain.GrassTiles[x, y].NextWateringCheck - 1;
                    continue;
                }
                else
                {
                    terrain.GrassTiles[x, y].NextWateringCheck = 10;
                }

                int index = (terrain.GrassTiles[x, y].NextTileToCheck + 1) % TileCoordinate.Length;
                terrain.GrassTiles[x, y].NextTileToCheck = (byte)index;
                Vector2I which = new Vector2I(x, y) + TileCoordinate[index];
                if (which.X >= 0 && which.Y >= 0 && which.X < terrain.Columns && which.Y < terrain.Rows)
                {
                    if (terrain.Tiles[which.X, which.Y].WaterHeight > 0)
                    {
                        terrain.Tiles[which.X, which.Y].WaterHeight = (byte)(terrain.Tiles[which.X, which.Y].WaterHeight > 0 ? terrain.Tiles[which.X, which.Y].WaterHeight - 1 : 0);
                        HydrateRegion(terrain, index, x, y, DEFAULT_HYDRATION);
                    }
                    else if (terrain.GrassTiles[which.X, which.Y].HumidFor >= DEFAULT_HYDRATION / 4)
                    {
                        HydrateRegion(terrain, index, x, y, terrain.GrassTiles[which.X, which.Y].HumidFor / 2 - 1);
                    }
                }
            }
        }
    }
}
