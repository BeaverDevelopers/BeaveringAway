using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Game : Node
{
    [Export] public Node2D WorldNode;
    
    [Export(PropertyHint.Range, "1,500,1")] public int JunkSpawnInterval = 100;
    [Export(PropertyHint.Range, "1,50,1")] public int JunkMaxCount = 5;
    [Export(PropertyHint.Range, "0.05,2.0,0.05")] public float JunkDriftSpeed = 0.35f;
    [Export] public ItemData JunkToSpawn;

    [Export] public GameHUD hud;
    public PlayerMove Player;

    public Camera2D MainCamera;

    Simulator simulator;
    JunkSystem junkSystem = new();

    // Deer spawning system
    private PackedScene deerScene;
    private float deerSpawnTimer = 0f;
    [Export(PropertyHint.Range, "1,60,1")] public float DeerSpawnInterval = 20f; // every 20 seconds spawn a deer
    [Export(PropertyHint.Range, "0,2,1")] public int DeerMaxHerds = 2;
    [Export(PropertyHint.Range, "1,3,1")] public int HerdMinFemales = 1;
    [Export(PropertyHint.Range, "1,3,1")] public int HerdMaxFemales = 3;
    [Export(PropertyHint.Range, "100,800,10")] public float DeerSpawnDistance = 400f;
    [Export(PropertyHint.Range, "20,150,5")] public float HerdFemaleSpawnRadius = 70f;
    [Export(PropertyHint.Range, "40,200,5")] public float BerryBushDeerSpawnRadius = 100f;
    [Export(PropertyHint.Range, "60,300,10")] public float BerryBushNearDistance = 180f;
    private int _nextHerdId = 0;

    // Fox spawning system
    private PackedScene foxScene;
    private float foxSpawnTimer = 0f;
    [Export(PropertyHint.Range, "1,60,1")] public float FoxSpawnInterval = 25f;
    [Export(PropertyHint.Range, "0,10,1")] public int FoxMaxCount = 2;
    [Export(PropertyHint.Range, "100,800,10")] public float FoxSpawnDistance = 400f;

    TileMapLayer waterLayer;
    TileMapLayer obstructionLayer;
    TileMapLayer terrainLayer;


    Vector2I WATER_CENTER = new Vector2I(0, 0);
    Vector2I WATER_DANGEROUS = new Vector2I(0, 0);
    Vector2I DAM_TILE = new Vector2I(0, 0);

    const int DAM_SOURCE_ID = 81;
    const int WATER_SOURCE_ID = 80;
    static readonly Vector2I WATER_SOURCE_TILE = new(71, 0);

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                Player.MoveTarget = MainCamera.GetGlobalMousePosition();
                Player.DigOnArrival = false;
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                Player.MoveTarget = MainCamera.GetGlobalMousePosition();
                Player.DigOnArrival = true;
            }
            
        }
    }

    public override void _Ready()
    {
        Debug.WriteLine("Loading Game");
        waterLayer = WorldNode.GetNode<TileMapLayer>("Level_0/Water");
        waterLayer.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        junkSystem.Initialize(waterLayer);
        junkSystem.SpawnInterval = JunkSpawnInterval;
        junkSystem.MaxItems = JunkMaxCount;
        junkSystem.DriftSpeed = JunkDriftSpeed;
        junkSystem.JunkToSpawn = JunkToSpawn;

        var tileSize = waterLayer.TileSet.TileSize;

        // Not needed for now.
        WorldNode.GetNode<TileMapLayer>("Level_0/Water_Decoration").Visible = false;

        obstructionLayer = WorldNode.GetNode<TileMapLayer>("Level_0/Obstructions");
        terrainLayer = WorldNode.GetNode<TileMapLayer>("Level_0/Terrain");

        simulator.Load(WorldNode);

        int mapW = simulator.Terrain.Columns * tileSize.X;
        int mapH = simulator.Terrain.Rows * tileSize.Y;
        hud.SetMapBounds(mapW, mapH);

        Player = GetNode<PlayerMove>("world/Player");
        MainCamera = Player.GetNode<Camera2D>("Camera2D");

        // Try to load Deer scene, but don't fail if it doesn't exist
        deerScene = GD.Load<PackedScene>("res://deer/deer.tscn");
        if (deerScene == null)
            deerScene = GD.Load<PackedScene>("res://deer/Deer.tscn");
        if (deerScene == null)
            Debug.WriteLine("Info: deer.tscn not found under res://deer/");
        else
            Debug.WriteLine("Successfully loaded deer scene");

        foxScene = GD.Load<PackedScene>("res://fox/fox.tscn");
        if (foxScene == null)
            Debug.WriteLine("Info: fox.tscn not found.");
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
                    obstructionLayer.SetCell(coords, DAM_SOURCE_ID, DAM_TILE);
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

    public void DigOnPosition(Vector2 pos)
    {
        var mapPos = obstructionLayer.LocalToMap(pos);
        simulator.Terrain.Tiles[mapPos.X, mapPos.Y].GroundHeight = Terrain.MUDFLOOR_TILE_HEIGHT;
        simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight = 0;
        terrainLayer.SetCell(mapPos, -1); // TODO: Fix this ugly hack.

        // Consume player energy for digging
        if (Player != null)
        {
            Player.ConsumeEnergy(Player.DigEnergyCost);
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
            DigOnPosition(MainCamera.GetGlobalMousePosition());
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

    public int PlaceItemUnderPlayer(ItemData item)
    {

        if (item.ItemId == 2)
        {
            var mapPos = obstructionLayer.LocalToMap(Player.Position);
            Debug.WriteLine(mapPos);
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight = (byte)Math.Min(simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight + 1, 4);
            simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHealth = 100;
            return 1;
        }

        if (item.ItemId == 5)
        {
            var sapplingScene = GD.Load<PackedScene>("res://scenes/tree.tscn");
            var itemScene = sapplingScene.Instantiate<Node2D>();
            WorldNode.AddChild(itemScene);
            itemScene.GlobalPosition = Player.Position;
            return 1;
        }


        //if the item is a hut it should only be placed in water
        if (item.ItemId == 3)
        {
            var mapPos = Player.Position;
            var game = GetTree().CurrentScene as Game;
            if (!game.IsPositionInWater(mapPos))
            {
                return 1;
            }

        }

        var playerPos = Player.Position;
        for (int i = 0; i < item.ItemCount; i++)
        {
            var itemScene = item.ItemScene.Instantiate<Node2D>();
            if (itemScene is DroppedItem droppedItem)
            {
                droppedItem.ItemData = item.Duplicate() as ItemData;
                droppedItem.ItemData.ItemCount = 1;
            }
            WorldNode.AddChild(itemScene);
            itemScene.GlobalPosition = playerPos;

        }

        return item.ItemCount;
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
        UpdateFoxSpawning((float)delta);

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

        var itemToPlace = Player.ItemToPlace;
        if (itemToPlace != null && Player.MoveTarget == Vector2.Zero)
        {
            if (itemToPlace.ItemCount > 0)
            {
                var itemsUsed = PlaceItemUnderPlayer(itemToPlace);
                itemToPlace.ItemCount -= itemsUsed;
                var inventory = InventoryDataNew.GetActiveMainInventory();
                if (itemToPlace.ItemCount < 0)
                {
                    inventory.TryRemoveItem(itemToPlace.ItemId, -itemToPlace.ItemCount);
                    itemToPlace.ItemCount = 0;
                }
                inventory.PurgeEmptyStacks();
            }
            Player.ItemToPlace = null;
        }
        if (Player.DigOnArrival && Player.MoveTarget == Vector2.Zero)
        {
            Player.DigOnArrival = false;

            // Check if player has enough energy to dig
            if (Player.HasEnoughEnergy(Player.DigEnergyCost))
            {
                Player.PlayDigSound();
                DigOnPosition(Player.Position);
            }
            else
            {
                Debug.WriteLine("Not enough energy to dig!");
            }
        }

        //Switching trees from dead to alive
        var trees = GetTree().GetNodesInGroup("trees");
        foreach (var node in trees)
        {
            if (node is StaticBody2D tree)
            {
                var treePos = obstructionLayer.LocalToMap(obstructionLayer.ToLocal(tree.GlobalTransform.Origin));
                bool alive = simulator.Terrain.GrassTiles[treePos.X, treePos.Y].HumidFor > 0;
                tree.Call("set_alive", alive);

            }        
        }

        //switching berry bushes from dead to alive
        var bushes = GetTree().GetNodesInGroup("bush");
        foreach (var node in bushes)
        {
            if (node is StaticBody2D bush)
            {
                var bushPos = obstructionLayer.LocalToMap(obstructionLayer.ToLocal(bush.GlobalTransform.Origin));
                bool alive = simulator.Terrain.GrassTiles[bushPos.X, bushPos.Y].HumidFor > 0;
                bush.Call("set_alive", alive);

            }        
        }
    }

    public bool IsPositionInWater(Vector2 globalPosition)
    {
        if (obstructionLayer == null) return false;

        var localPos = obstructionLayer.ToLocal(globalPosition);
        var mapPos = obstructionLayer.LocalToMap(localPos);
        if (mapPos.X < 0 || mapPos.Y < 0 || mapPos.X >= simulator.Terrain.Columns || mapPos.Y >= simulator.Terrain.Rows)
            return false;

        return simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight > 0;
    }

    public bool IsPositionOnGrass(Vector2 globalPosition)
    {
        if (obstructionLayer == null) return false;

        var localPos = obstructionLayer.ToLocal(globalPosition);
        var mapPos = obstructionLayer.LocalToMap(localPos);
        if (mapPos.X < 0 || mapPos.Y < 0 || mapPos.X >= simulator.Terrain.Columns || mapPos.Y >= simulator.Terrain.Rows)
            return false;

        // Check if there's grass at this location
        // Grass tiles exist when HumidFor > 0 (grass is still alive/visible)
        var grassTile = simulator.Terrain.GrassTiles[mapPos.X, mapPos.Y];
        return grassTile.HumidFor > 0;
    }

    private void UpdateDeerSpawning(float delta)
    {
        if (deerScene == null) return;

        deerSpawnTimer += delta;
        if (deerSpawnTimer < DeerSpawnInterval)
            return;

        deerSpawnTimer = 0f;

        TryCompleteIncompleteHerds();

        if (CountDeerHerds() < DeerMaxHerds)
            SpawnDeerHerd();
    }

    private int CountDeerHerds()
    {
        var count = 0;
        foreach (var node in GetTree().GetNodesInGroup("deer"))
        {
            if (node is DeerMovement deer && deer.IsHerdMale)
                count++;
        }

        return count;
    }

    private void TryCompleteIncompleteHerds()
    {
        if (deerScene == null)
            return;

        foreach (var node in GetTree().GetNodesInGroup("deer"))
        {
            if (node is not DeerMovement male || !male.IsHerdMale)
                continue;

            if (male.HerdFemaleCount >= HerdMinFemales)
                continue;

            int femalesToAdd = HerdMinFemales - male.HerdFemaleCount;
            for (int i = 0; i < femalesToAdd; i++)
                TryAddFemaleToHerd(male);
        }
    }

    private void SpawnDeerHerd()
    {
        if (deerScene == null || Player == null)
            return;

        int herdId = _nextHerdId++;
        if (!TryPickAliveBerryBushCenter(Player.GlobalPosition, DeerSpawnDistance, out var bushCenter))
        {
            Debug.WriteLine($"No alive berry bushes near player for deer herd {herdId}");
            return;
        }

        var maleDeer = TrySpawnDeer(DeerMovement.Gender.Male, herdId, bushCenter, BerryBushDeerSpawnRadius);
        if (maleDeer == null)
        {
            Debug.WriteLine($"Failed to spawn male deer near berry bush for herd {herdId}");
            return;
        }

        var rng = new RandomNumberGenerator();
        int targetFemales = rng.RandiRange(HerdMinFemales, HerdMaxFemales);
        int spawnedFemales = 0;

        for (int i = 0; i < targetFemales; i++)
        {
            if (TryAddFemaleToHerd(maleDeer))
                spawnedFemales++;
        }

        for (int attempt = 0; attempt < 30 && spawnedFemales < HerdMinFemales; attempt++)
        {
            if (TryAddFemaleToHerd(maleDeer))
                spawnedFemales++;
        }

        if (spawnedFemales < HerdMinFemales)
            Debug.WriteLine($"Herd {herdId}: male spawned, still need {HerdMinFemales - spawnedFemales} more females");
        else
            Debug.WriteLine($"Deer herd {herdId} spawned: 1 male + {spawnedFemales} females");
    }

    private bool TryAddFemaleToHerd(DeerMovement maleDeer)
    {
        if (maleDeer == null || maleDeer.HerdFemaleCount >= HerdMaxFemales)
            return false;

        var femaleDeer = TrySpawnDeer(
            DeerMovement.Gender.Female,
            maleDeer.HerdId,
            maleDeer.GlobalPosition,
            HerdFemaleSpawnRadius,
            maleDeer);

        if (femaleDeer == null)
            return false;

        maleDeer.AddFemaleToHerd(femaleDeer);
        return true;
    }

    private DeerMovement TrySpawnDeer(
        DeerMovement.Gender gender,
        int herdId,
        Vector2 center,
        float radius,
        DeerMovement herdMale = null)
    {
        if (deerScene == null)
            return null;

        if (gender == DeerMovement.Gender.Female && herdMale == null)
            return null;

        if (!TryFindGrassNearBerryBushPosition(center, radius, out var spawnPos))
            return null;

        var deer = deerScene.Instantiate() as DeerMovement;
        if (deer == null)
            return null;

        if (gender == DeerMovement.Gender.Male)
            deer.ConfigureHerdMale(herdId);
        else
            deer.ConfigureHerdFemale(herdId, herdMale);

        WorldNode.AddChild(deer);
        deer.GlobalPosition = spawnPos;
        deer.AddToGroup("deer");
        return deer;
    }

    private List<Vector2> CollectAliveBerryBushPositions()
    {
        var positions = new List<Vector2>();
        if (obstructionLayer == null)
            return positions;

        foreach (var node in GetTree().GetNodesInGroup("bush"))
        {
            if (node is not Node2D bush)
                continue;

            var mapPos = obstructionLayer.LocalToMap(obstructionLayer.ToLocal(bush.GlobalPosition));
            if (mapPos.X < 0 || mapPos.Y < 0
                || mapPos.X >= simulator.Terrain.Columns
                || mapPos.Y >= simulator.Terrain.Rows)
                continue;

            if (simulator.Terrain.GrassTiles[mapPos.X, mapPos.Y].HumidFor > 0)
                positions.Add(bush.GlobalPosition);
        }

        return positions;
    }

    private bool TryPickAliveBerryBushCenter(Vector2 nearPoint, float searchRadius, out Vector2 bushCenter)
    {
        bushCenter = default;
        var aliveBushes = CollectAliveBerryBushPositions();
        if (aliveBushes.Count == 0)
            return false;

        var inRange = new List<Vector2>();
        foreach (var position in aliveBushes)
        {
            if (nearPoint.DistanceTo(position) <= searchRadius)
                inRange.Add(position);
        }

        var pool = inRange.Count > 0 ? inRange : aliveBushes;
        var rng = new RandomNumberGenerator();
        bushCenter = pool[rng.RandiRange(0, pool.Count - 1)];
        return true;
    }

    private bool IsNearAliveBerryBush(Vector2 globalPosition, IReadOnlyList<Vector2> aliveBushes)
    {
        foreach (var bushPosition in aliveBushes)
        {
            if (globalPosition.DistanceTo(bushPosition) <= BerryBushNearDistance)
                return true;
        }

        return false;
    }

    private bool TryFindGrassNearBerryBushPosition(Vector2 center, float radius, out Vector2 spawnPos)
    {
        var aliveBushes = CollectAliveBerryBushPositions();
        if (aliveBushes.Count == 0)
        {
            spawnPos = default;
            return false;
        }

        var terrain = simulator.Terrain;
        int mapW = terrain.Columns * 16;
        int mapH = terrain.Rows * 16;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float angle = (float)(GD.Randf() * Mathf.Tau);
            float distance = radius <= 0f ? 0f : (float)GD.Randf() * radius;
            spawnPos = center + Vector2.FromAngle(angle) * distance;
            spawnPos.X = Mathf.Clamp(spawnPos.X, 0, mapW);
            spawnPos.Y = Mathf.Clamp(spawnPos.Y, 0, mapH);

            if (!IsPositionInWater(spawnPos)
                && IsPositionOnGrass(spawnPos)
                && IsNearAliveBerryBush(spawnPos, aliveBushes))
                return true;
        }

        spawnPos = default;
        return false;
    }

    private void UpdateFoxSpawning(float delta)
    {
        if (foxScene == null || Player == null)
            return;

        foxSpawnTimer += delta;
        if (foxSpawnTimer < FoxSpawnInterval)
            return;

        foxSpawnTimer = 0f;

        if (!InventoryData.HasAnyItems())
            return;

        var existingFoxes = GetTree().GetNodesInGroup("fox");
        if (existingFoxes.Count >= FoxMaxCount)
            return;

        SpawnFox();
    }

    private void SpawnFox()
    {
        if (foxScene == null || Player == null || !InventoryData.HasAnyItems())
            return;

        if (!TryGetSpawnPositionAroundPlayer(FoxSpawnDistance, avoidWater: true, out var spawnPos))
            return;

        var fox = foxScene.Instantiate() as FoxMovement;
        if (fox == null)
            return;

        WorldNode.AddChild(fox);
        fox.GlobalPosition = spawnPos;
        Debug.WriteLine($"Fox spawned at {spawnPos}");
    }

    private bool TryGetSpawnPositionAroundPlayer(float spawnDistance, bool avoidWater, out Vector2 spawnPos)
    {
        var terrain = simulator.Terrain;
        int mapW = terrain.Columns * 16;
        int mapH = terrain.Rows * 16;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = (float)(GD.Randf() * Mathf.Tau);
            Vector2 candidate = Player.GlobalPosition + Vector2.FromAngle(angle) * spawnDistance;
            candidate.X = Mathf.Clamp(candidate.X, 0, mapW);
            candidate.Y = Mathf.Clamp(candidate.Y, 0, mapH);

            if (avoidWater && IsPositionInWater(candidate))
                continue;

            spawnPos = candidate;
            return true;
        }

        spawnPos = default;
        return false;
    }
}
