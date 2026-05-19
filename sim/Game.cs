using Godot;
using System;
using System.Diagnostics;

public partial class Game : Node
{
    [Export] public Node MapNode;
    
    [Export(PropertyHint.Range, "1,500,1")] public int JunkSpawnInterval = 100;
    [Export(PropertyHint.Range, "1,50,1")] public int JunkMaxCount = 5;
    [Export(PropertyHint.Range, "0.05,2.0,0.05")] public float JunkDriftSpeed = 0.35f;

    [Export] public GameHUD hud;
    public PlayerMove Player;

    public Camera2D MainCamera;

    Simulator simulator;
    JunkSystem junkSystem = new();

    // Deer spawning system
    private PackedScene deerScene;
    private float deerSpawnTimer = 0f;
    [Export(PropertyHint.Range, "1,60,1")] public float DeerSpawnInterval = 20f; // 每20秒尝试生成一次
    [Export(PropertyHint.Range, "0,10,1")] public int DeerMaxCount = 3; // 最多3只鹿

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
        Debug.WriteLine("Loading Game");
        waterLayer = MapNode.GetNode<TileMapLayer>("Level_0/Water");
        waterLayer.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        junkSystem.Initialize(waterLayer);
        junkSystem.SpawnInterval = JunkSpawnInterval;
        junkSystem.MaxItems = JunkMaxCount;
        junkSystem.DriftSpeed = JunkDriftSpeed;

        var tileSize = waterLayer.TileSet.TileSize;

        // Not needed for now.
        MapNode.GetNode<TileMapLayer>("Level_0/Water_Decoration").Visible = false;

        obstructionLayer = MapNode.GetNode<TileMapLayer>("Level_0/Obstructions");
        terrainLayer = MapNode.GetNode<TileMapLayer>("Level_0/Terrain");

        simulator.Load(MapNode);

        int mapW = simulator.Terrain.Columns * tileSize.X;
        int mapH = simulator.Terrain.Rows * tileSize.Y;
        hud.SetMapBounds(mapW, mapH);

        Player = GetNode<PlayerMove>("Player");
        MainCamera = Player.GetNode<Camera2D>("Camera2D");

        // Try to load Deer scene, but don't fail if it doesn't exist
        deerScene = GD.Load<PackedScene>("res://deer/Deer.tscn");
        if (deerScene == null)
        {
            Debug.WriteLine("Info: Deer.tscn not found. Please create the Deer scene and save it to res://deer/Deer.tscn");
            Debug.WriteLine("      Or add a Deer prefab to your project for dynamic spawning.");
        }
        else
        {
            Debug.WriteLine("Successfully loaded Deer scene from res://deer/Deer.tscn");
        }
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

            for (int i = 0; i < simulator.Terrain.ProcessingOrder.Length; i++)
            {
                if (simulator.Terrain.ProcessingOrder[i] == mapPos)
                {
                    Debug.WriteLine("Processing number: " + i);
                }
            }
        }
    }

    public void PlaceDam()
    {
        Debug.WriteLine("placing dam");
        var mapPos = obstructionLayer.LocalToMap(MainCamera.GetGlobalMousePosition());
        Debug.WriteLine(mapPos);
        simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight = (byte)Math.Min(simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight + 1, 4);
        simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHealth = 100;
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

        // Update Deer spawning
        UpdateDeerSpawning((float)delta);

        simulator.Run();
        junkSystem.Spawn(simulator.Terrain, WATER_SOURCE_TILE, simulator.Tick);
        junkSystem.Update(simulator.Terrain, simulator.Tick);

        RenderWaterAndObstructions(simulator.Terrain);

        // Demo HUD values (replace with real game logic later)
        hud.UpdateSeason(simulator.Tick);
        hud.UpdateHunger(100 - (simulator.Tick % 6000) / 60f);
        float seasonProgress = (simulator.Tick % 14400) / 14400f;
        hud.UpdateTemperature(Mathf.Lerp(-10f, 35f, Mathf.Sin(seasonProgress * Mathf.Tau) * 0.5f + 0.5f));

        if (obstructionLayer != null && Player != null)
        {
            var currentCell = obstructionLayer.LocalToMap(new Vector2I((int)Player.Position.X, (int)Player.Position.Y));
            if (currentCell.X >= 0 && currentCell.Y >= 0 && currentCell.X < simulator.Terrain.Columns && currentCell.Y < simulator.Terrain.Rows)
            {
                Player.InWater = simulator.Terrain.Tiles[currentCell.X, currentCell.Y].WaterHeight > 0;
            }
        }
    }

    private void UpdateDeerSpawning(float delta)
    {
        if (deerScene == null) return;

        deerSpawnTimer += delta;
        if (deerSpawnTimer >= DeerSpawnInterval)
        {
            deerSpawnTimer = 0f;

            // Check current number of deer in the scene
            var existingDeer = GetTree().GetNodesInGroup("deer");
            if (existingDeer.Count < DeerMaxCount)
            {
                SpawnDeer();
            }
        }
    }

    private void SpawnDeer()
    {
        if (deerScene == null || Player == null) return;

        // Instantiate the deer from the scene
        var deer = deerScene.Instantiate() as DeerMovement;
        if (deer == null) return;

        // Add to scene
        MapNode.AddChild(deer);

        // Spawn at a random position around the player (within a certain range)
        float spawnDistance = 400f; // 距离玩家400像素范围内生成
        float angle = (float)(GD.Randf() * Mathf.Tau); // 随机角度
        Vector2 spawnOffset = Vector2.FromAngle(angle) * spawnDistance;
        Vector2 spawnPos = Player.GlobalPosition + spawnOffset;

        // Clamp to map bounds
        var terrain = simulator.Terrain;
        int mapW = terrain.Columns * 16; // 假设瓦片大小为16
        int mapH = terrain.Rows * 16;

        spawnPos.X = Mathf.Clamp(spawnPos.X, 0, mapW);
        spawnPos.Y = Mathf.Clamp(spawnPos.Y, 0, mapH);

        deer.GlobalPosition = spawnPos;
        deer.AddToGroup("deer");

        Debug.WriteLine($"Deer spawned at {spawnPos}");
    }
}
