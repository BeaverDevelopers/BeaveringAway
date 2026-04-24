using System;
using System.Diagnostics;

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
            var doSpread = (tick % 3 == 1);
            if (i > 0 || doSpread)
            {
                var newDirection = (tick * 7 + x * 5 + y * 3 + i) % 5;
                if (newDirection == 0)
                {
                    direction = WaterDirection.Down;
                }
                else if (newDirection == 1)
                {
                    direction = WaterDirection.Up;
                }
                else if (newDirection == 2)
                {
                    direction = WaterDirection.Right;
                }
                else if (newDirection == 3)
                {
                    direction = WaterDirection.Left;
                }
                else if (newDirection == 4)
                {
                    direction = WaterDirection.None;
                }
            }

            // TODO: Use a match statement or something instead.
            if (direction == WaterDirection.Up)
            {
                terrain.MoveWater(x, y, x, y - 1, WaterDirection.Up, false);
            }
            if (direction == WaterDirection.Down)
            {
                terrain.MoveWater(x, y, x, y + 1, WaterDirection.Down, doSpread);
            }
            if (direction == WaterDirection.Left)
            {
                terrain.MoveWater(x, y, x - 1, y, WaterDirection.Left, false);
            }
            if (direction == WaterDirection.Right)
            {
                terrain.MoveWater(x, y, x + 1, y, WaterDirection.Right, false);
            }

            if (i == 0)
            {
                terrain.Tiles[x, y].WaterVelocity = direction;
            }
        }
    }

    public static int Pacify(Terrain terrain, int x, int y)
    {
        if (x < 0 || y < 0 || x >= terrain.Columns || y >= terrain.Rows)
        {
            return 0;
        }

        sbyte to = -5;

        int change = terrain.Tiles[x, y].DangerLevel - to;

        terrain.Tiles[x, y].DangerLevel = to;

        if (change < 0)
        {
            return 0;
        }

        return change;
    }

    public static void Run(Terrain terrain, int tick)
    {
        int grandTotalPacified = 0;

        // Reset some variables from previous runs.
        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                // Simulate the ground soaking some water.
                if ((tick * 2 + x * 71 + y * 33) % 2400 == 0)
                {
                    terrain.Tiles[x, y].WaterHeight = (byte)Math.Max(0, terrain.Tiles[x, y].WaterHeight - 1);
                }

                if (terrain.Tiles[x, y].WaterShown < 3 && terrain.Tiles[x, y].DangerLevel > 0)
                {
                    terrain.Tiles[x, y].DangerLevel -= 1;
                }

                // Reset per-tick state.
                terrain.Tiles[x, y].Locked = false;
                terrain.WaterTiles[x, y].FlowX = 0;
                terrain.WaterTiles[x, y].FlowY = 0;
                if (terrain.Tiles[x, y].WaterHeight == 0)
                {
                    terrain.Tiles[x, y].WaterVelocity = WaterDirection.None;
                    if (terrain.Tiles[x, y].WaterShown > 0)
                    {
                        terrain.Tiles[x, y].WaterShown--;
                    }
                }
                else
                {
                    terrain.Tiles[x, y].WaterShown = (byte)Math.Max((int)terrain.Tiles[x, y].WaterShown, 1);
                }

                if (terrain.Tiles[x, y].ObstructionHeight > 0)
                {
                    var totalPacification = 0;

                    // If we are under water, we can only pacify the current tile.
                    totalPacification += Pacify(terrain, x, y);

                    // Pacifying the surrounding since we are above water.
                    if (terrain.Tiles[x, y].WaterHeight == 0)
                    {
                        totalPacification += Pacify(terrain, x, y - 1);
                        totalPacification += Pacify(terrain, x, y + 1);
                        totalPacification += Pacify(terrain, x - 1, y);
                        totalPacification += Pacify(terrain, x + 1, y);
                    }

                    if (totalPacification > 0)
                    {
                        terrain.DamageTile(x, y, totalPacification);
                    }

                    grandTotalPacified += totalPacification;

                }
            }
        }

        for (int y = terrain.Rows - 1; y >= 0; y--)
        {
            // We don't want to pick a favorite side. So we alternate.
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

        // Smooth the flow field to reduce jitter from CA randomization.
        const float Smoothing = 0.3f;
        int totalWater = 0;
        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                terrain.WaterTiles[x, y].SmoothedFlowX += (terrain.WaterTiles[x, y].FlowX - terrain.WaterTiles[x, y].SmoothedFlowX) * Smoothing;
                terrain.WaterTiles[x, y].SmoothedFlowY += (terrain.WaterTiles[x, y].FlowY - terrain.WaterTiles[x, y].SmoothedFlowY) * Smoothing;
                totalWater += terrain.Tiles[x, y].WaterHeight;
            }
        }

        if (tick % 100 == 0)
        {
            Debug.WriteLine("Total water: " + totalWater + " pacified: " + grandTotalPacified);
        }
    }
}
