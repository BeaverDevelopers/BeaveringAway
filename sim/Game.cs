using Godot;
using System;
using System.Diagnostics;

public partial class Game : Node
{
    [Export] public Node MapNode;
    [Export] public PlayerMove Player;
    [Export(PropertyHint.Range, "1,500,1")] public int JunkSpawnInterval = 100;
    [Export(PropertyHint.Range, "1,50,1")] public int JunkMaxCount = 5;
    [Export(PropertyHint.Range, "0.05,2.0,0.05")] public float JunkDriftSpeed = 0.35f;

    [Export] public GameHUD hud;

    public Camera2D MainCamera;

    Simulator simulator;
    JunkSystem junkSystem = new();

    TileMapLayer waterLayer;
    TileMapLayer obstructionLayer;
    TileMapLayer terrainLayer;

    Vector2I WATER_CENTER = new Vector2I(5, 17);
    Vector2I WATER_DANGEROUS = new Vector2I(15, 19);
    Vector2I DAM_TILE = new Vector2I(14, 12);

    const int WATER_SOURCE_ID = 2;
    static readonly Vector2I WATER_SOURCE_TILE = new(71, 0);

    public override void _Ready()
    {
        simulator.Load(MapNode);
        waterLayer = MapNode.GetNode<TileMapLayer>("Level_0/Water");
        waterLayer.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        junkSystem.Initialize(waterLayer);
        junkSystem.SpawnInterval = JunkSpawnInterval;
        junkSystem.MaxItems = JunkMaxCount;
        junkSystem.DriftSpeed = JunkDriftSpeed;

        var tileSize = waterLayer.TileSet.TileSize;
        int mapW = simulator.Terrain.Columns * tileSize.X;
        int mapH = simulator.Terrain.Rows * tileSize.Y;
        hud.SetMapBounds(mapW, mapH);

        // Not needed for now.
        MapNode.GetNode<TileMapLayer>("Level_0/Water_Decoration").Visible = false;

        obstructionLayer = MapNode.GetNode<TileMapLayer>("Level_0/Obstructions");
        terrainLayer = MapNode.GetNode<TileMapLayer>("Level_0/Terrain");


    }

    void RenderWaterAndObstructions(Terrain terrain)
    {
        for (int y = 0; y < terrain.Rows - 1; y++)
        {
            for (int x = 0; x < terrain.Columns; x++)
            {
                var tile = terrain.Tiles[x, y];
                var coords = new Vector2I(x, y);
                if (tile.WaterHeight > 0)
                {
                    if (tile.DangerLevel > 5)
                    {
                        waterLayer.SetCell(coords, WATER_SOURCE_ID, WATER_DANGEROUS);
                    }
                    else
                    {
                        waterLayer.SetCell(coords, WATER_SOURCE_ID, WATER_CENTER);
                    }

                }
                else
                {
                    waterLayer.SetCell(coords, -1, WATER_CENTER);
                }

                if (tile.ObstructionHeight > 0)
                {
                    obstructionLayer.SetCell(coords, 6, DAM_TILE);
                }
                else
                {
                    obstructionLayer.SetCell(coords, -1, DAM_TILE);

                }

                var grass = terrain.GrassTiles[x, y];

                var atlasCoords = terrainLayer.GetCellAtlasCoords(coords);
                int sourceID = grass.HumidFor > 0 ? 77 : 40; 
                terrainLayer.SetCell(coords, sourceID, atlasCoords);
                
            }
        }
    }

    public void RunDebugCommands()
    {
        if (Input.IsPhysicalKeyPressed(Key.V))
        {
            Debug.WriteLine("Give us some water!");
            var mapPos = obstructionLayer.LocalToMap(MainCamera.GetGlobalMousePosition());
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight = Math.Max(simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight, (byte)15);
        }
        if (Input.IsPhysicalKeyPressed(Key.R))
        {
            for (int y = 0; y < simulator.Terrain.Rows; y++)
            {
                for (int x = 0; x < simulator.Terrain.Columns; x++)
                {
                    simulator.Terrain.Tiles[x, y].WaterHeight = 0;
                    //simulator.Terrain.Tiles[x, y].WaterVelocity = WaterDirection.None;
                }
            }
            junkSystem.ClearAll();
        }

        if (Input.IsPhysicalKeyPressed(Key.P))
        {
            var mapPos = obstructionLayer.LocalToMap(MainCamera.GetGlobalMousePosition());
            Debug.WriteLine(mapPos);
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight = (byte)Math.Min(simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight + 1, 4);
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHealth = 100;
        }

        if (Input.IsPhysicalKeyPressed(Key.L))
        {
            var mapPos = obstructionLayer.LocalToMap(MainCamera.GetGlobalMousePosition());
            Debug.WriteLine(mapPos);
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].GroundHeight = Terrain.MUDFLOOR_TILE_HEIGHT;
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight = 0;
            terrainLayer.SetCell(mapPos, -1); // TODO: Fix this ugly hack.
        }

        if (Input.IsPhysicalKeyPressed(Key.N))
        {
            var mapPos = obstructionLayer.LocalToMap(MainCamera.GetGlobalMousePosition());
            Debug.WriteLine(mapPos);
            Debug.WriteLine("Water height: " + simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight);
            Debug.WriteLine("Terrain height: " + simulator.Terrain.Tiles[mapPos.X, mapPos.Y].GroundHeight);
            Debug.WriteLine("Sea distance: " + simulator.Terrain.WaterRandomTickData[mapPos.X, mapPos.Y].DistanceToSea);
            Debug.WriteLine("Next soak: " + simulator.Terrain.WaterRandomTickData[mapPos.X, mapPos.Y].NextSoak);
            Debug.WriteLine("Humid for: " + simulator.Terrain.GrassTiles[mapPos.X, mapPos.Y].HumidFor);
            Debug.WriteLine("Next water check: " + simulator.Terrain.GrassTiles[mapPos.X, mapPos.Y].NextWateringCheck);
            Debug.WriteLine("Water pos:" + GrassSimulation.TileCoordinate[simulator.Terrain.GrassTiles[mapPos.X, mapPos.Y].NextTileToCheck]);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (MainCamera == null)
        {
            Debug.WriteLine("Waiting for MainCamera to appear");
            return;
        }
        // TODO: This is all crap code that I added to debug the water sim.

        //simulator.Terrain.Tiles[9, 0].WaterHeight = 2;
        if (simulator.Tick % 5 == 0)
        {
            simulator.Terrain.Tiles[WATER_SOURCE_TILE.X, WATER_SOURCE_TILE.Y].WaterHeight =
                Math.Max(simulator.Terrain.Tiles[WATER_SOURCE_TILE.X, WATER_SOURCE_TILE.Y].WaterHeight, (byte)12);
            simulator.Terrain.Tiles[WATER_SOURCE_TILE.X - 1, WATER_SOURCE_TILE.Y].WaterHeight =
                Math.Max(simulator.Terrain.Tiles[WATER_SOURCE_TILE.X - 1, WATER_SOURCE_TILE.Y].WaterHeight, (byte)12);
            simulator.Terrain.Tiles[WATER_SOURCE_TILE.X + 1, WATER_SOURCE_TILE.Y].WaterHeight =
                Math.Max(simulator.Terrain.Tiles[WATER_SOURCE_TILE.X + 1, WATER_SOURCE_TILE.Y].WaterHeight, (byte)12);
            //simulator.Terrain.Tiles[WATER_SOURCE_TILE.X, WATER_SOURCE_TILE.Y].DangerLevel = 10;
        }

        var DebugKeyPressed = Input.IsPhysicalKeyPressed(Key.M);
        if (DebugKeyPressed)
        {
            RunDebugCommands();
        }


        simulator.Run();
        junkSystem.Spawn(simulator.Terrain, WATER_SOURCE_TILE, simulator.Tick);
        junkSystem.Update(simulator.Terrain, simulator.Tick);

        RenderWaterAndObstructions(simulator.Terrain);

        // Demo HUD values (replace with real game logic later)
        hud.UpdateSeason(simulator.Tick);
        hud.UpdateHunger(100 - (simulator.Tick % 6000) / 60f);
        float seasonProgress = (simulator.Tick % 14400) / 14400f;
        hud.UpdateTemperature(Mathf.Lerp(-10f, 35f, Mathf.Sin(seasonProgress * Mathf.Tau) * 0.5f + 0.5f));

        var currentCell = obstructionLayer.LocalToMap(new Vector2I((int)Player.Position.X, (int)Player.Position.Y));
        if (currentCell.X >= 0 && currentCell.Y >= 0 && currentCell.X < simulator.Terrain.Columns && currentCell.Y < simulator.Terrain.Rows)
        Player.InWater = simulator.Terrain.Tiles[currentCell.X, currentCell.Y].WaterHeight > 0;
    }
}
