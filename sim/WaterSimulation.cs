using Godot;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;

public class WaterSimulation
{
    private static void simulateWaterTile(Terrain terrain, int x, int y, int tick)
    {
        var waterHeight = (int)terrain.Tiles[x, y].WaterHeight;

        if (terrain.Tiles[x, y].Locked)
        {
            return;
        }

        var adjacentWater = terrain.TileWater(x + 1, y) + terrain.TileWater(x - 1, y) + terrain.TileWater(x, y + 1) + terrain.TileWater(x, y - 1);

        var steps = Math.Min(waterHeight, 40);

        var direction = terrain.Tiles[x, y].WaterVelocity;
        for (int i = 0; i < steps; i++)
        {
            if (adjacentWater < 2)
            {
                direction = WaterVelocity.Down;
            }

            var doSpread = (tick % 3 == 1);
            if (i > 0 || doSpread)
            {
                var newDirection = (tick * 7 + x * 5 + y * 3 + i) % 5;
                if (newDirection == 0)
                {
                    direction = WaterVelocity.Down;
                }
                else if (newDirection == 1)
                {
                    direction = WaterVelocity.Up;
                }
                else if (newDirection == 2)
                {
                    direction = WaterVelocity.Right;
                }
                else if (newDirection == 3)
                {
                    direction = WaterVelocity.Left;
                }
                else if (newDirection == 4)
                {
                    direction = WaterVelocity.None;
                }
            }


            const int WATER_UNITS_TO_MOVE = 1;

            // TODO: Use a match statement or something instead.
            if (direction == WaterVelocity.Up)
            {
                terrain.MoveWater(x, y, x, y - 1, WATER_UNITS_TO_MOVE, WaterVelocity.Up, false);
            }
            if (direction == WaterVelocity.Down)
            {
                terrain.MoveWater(x, y, x, y + 1, WATER_UNITS_TO_MOVE, WaterVelocity.Down, true);
            }
            if (direction == WaterVelocity.Left)
            {
                terrain.MoveWater(x, y, x - 1, y, WATER_UNITS_TO_MOVE, WaterVelocity.Left, false);
            }
            if (direction == WaterVelocity.Right)
            {
                terrain.MoveWater(x, y, x + 1, y, WATER_UNITS_TO_MOVE, WaterVelocity.Right, false);
            }

            if (i == 0) {
                terrain.Tiles[x, y].WaterVelocity = direction;
            }
        }
    }

    public static void Run(Terrain terrain, int tick)
    {
        for (int y = terrain.Rows - 1; y >= 0; y--)
        {
            if ((tick + y) % 2 == 0)
            {
                for (int x = 0; x < terrain.Columns; x++)
                {
                    simulateWaterTile(terrain, x, y, tick);
                }
            }
            else
            {
                for (int x = terrain.Columns - 1; x >= 0; x--)
                {
                    simulateWaterTile(terrain, x, y, tick);
                }
            }
        }

        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                if (terrain.Tiles[x, y].WaterHeight == 0)
                {
                    terrain.Tiles[x, y].WaterVelocity = WaterVelocity.None;
                }
                terrain.Tiles[x, y].Locked = false;
            }
        }

        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                if ((tick * 2 + x * 71 + y * 33) % 2400 == 0)
                {
                    terrain.Tiles[x, y].WaterHeight = (byte)Math.Max(0, terrain.Tiles[x, y].WaterHeight - 1);
                }
            }
        }


        int totalWater = 0;
        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                totalWater += terrain.Tiles[x, y].WaterHeight;
            }
        }
        Debug.WriteLine("Total water: " + totalWater);
    }
}
