using System;
using System.Diagnostics;

public class WaterSimulation
{
    private static void SimulateWaterTile(Terrain terrain, int x, int y, int tick)
    {
        int waterHeight = ((int)terrain.Tiles[x, y].WaterHeight) - ((int)terrain.Tiles[x, y].WaterLocked);
        var steps = Math.Min(waterHeight, 40);

        var direction = terrain.Tiles[x, y].WaterDirection;
        for (int i = 0; i < steps; i++)
        {
            switch (direction) {
                case WaterDirection.Up:
                    terrain.MoveWater(x, y, x, y - 1, WaterDirection.Up, i == 0);
                    direction = WaterDirection.Right;
                    break;
                case WaterDirection.Down:
                    terrain.MoveWater(x, y, x, y + 1, WaterDirection.Down, i == 0);
                    direction = WaterDirection.Up;
                    break;
                case WaterDirection.Left:
                    terrain.MoveWater(x, y, x - 1, y, WaterDirection.Left, i == 0);
                    direction = WaterDirection.Down;
                    break;
                case WaterDirection.Right:
                    terrain.MoveWater(x, y, x + 1, y, WaterDirection.Right, i == 0);
                    direction = WaterDirection.Left;
                    break;
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
                if (terrain.Tiles[x, y].DangerLevel > 0)
                {
                    terrain.Tiles[x, y].DangerLevel -= 1;
                }

                // Reset per-tick state.
                terrain.WaterTiles[x, y].FlowX = 0;
                terrain.WaterTiles[x, y].FlowY = 0;

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

        // Slowly recalculate distance to sea.
        // And maybe soak water.
        for (int i = 0; i < 7350; i++)
        {
            int idx = (i + tick * 7350) % terrain.ProcessingOrder.Length;
            Godot.Vector2I pos = terrain.ProcessingOrder[idx];

            var currentDistance = terrain.WaterRandomTickData[pos.X, pos.Y].DistanceToSea;
            if (currentDistance == 0)
            {
                continue;
            }

            var up    = terrain.GetWaterRandomTickData(pos.X, pos.Y - 1).DistanceToSea;
            var down  = terrain.GetWaterRandomTickData(pos.X, pos.Y + 1).DistanceToSea;
            var left  = terrain.GetWaterRandomTickData(pos.X - 1, pos.Y).DistanceToSea;
            var right = terrain.GetWaterRandomTickData(pos.X + 1, pos.Y).DistanceToSea;

            if (terrain.Tiles[pos.X, pos.Y].GroundHeight == Terrain.MUDFLOOR_TILE_HEIGHT)
            {
                terrain.WaterRandomTickData[pos.X, pos.Y].DistanceToSea = Math.Max(1, Math.Min(Math.Min(Math.Min(up, down), Math.Min(left, right)) + 1, int.MaxValue / 2) + terrain.Tiles[pos.X, pos.Y].WaterHeight);
            }
            if (currentDistance > up && i % 4 != 0)
            {
                terrain.Tiles[pos.X, pos.Y].WaterDirection = WaterDirection.Up;
            }
            if (currentDistance > down && i % 4 != 1)
            {
                terrain.Tiles[pos.X, pos.Y].WaterDirection = WaterDirection.Down;
            }
            if (currentDistance > left && i % 4 != 2)
            {
                terrain.Tiles[pos.X, pos.Y].WaterDirection = WaterDirection.Left;
            }
            if (currentDistance > right && i % 4 != 3)
            {
                terrain.Tiles[pos.X, pos.Y].WaterDirection = WaterDirection.Right;
            }

            // Water soaking starts here.
            if (terrain.WaterRandomTickData[pos.X, pos.Y].NextSoak > 0)
            {
                terrain.WaterRandomTickData[pos.X, pos.Y].NextSoak--;
            }
            else
            {
                terrain.Tiles[pos.X, pos.Y].WaterHeight = (byte)(terrain.Tiles[pos.X, pos.Y].WaterHeight > 0 ? terrain.Tiles[pos.X, pos.Y].WaterHeight - 1 : 0);

                int nextSoakIn = 400;
                if (terrain.Tiles[pos.X, pos.Y].GroundHeight == Terrain.MUDFLOOR_TILE_HEIGHT)
                {
                    if (currentDistance <= int.MaxValue / 2)
                    {
                        nextSoakIn = 3200;
                    }
                    else
                    {
                        nextSoakIn = 800;
                    }
                }
                terrain.WaterRandomTickData[pos.X, pos.Y].NextSoak = nextSoakIn;
            }
        }

        // The bread and butter.
        for (int y = 0; y < terrain.Rows; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                if (terrain.WaterRandomTickData[x, y].DistanceToSea < 5)
                {
                    // We reachedd the sea. No need to render water anymore.
                    terrain.Tiles[x, y].WaterHeight = 0;
                }

                SimulateWaterTile(terrain, x, y, tick);

                // Free the water.
                terrain.Tiles[x, y].WaterLocked = 0;
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
