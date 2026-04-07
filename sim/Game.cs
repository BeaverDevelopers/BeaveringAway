using Godot;
using System;
using System.Diagnostics;

public partial class Game : Node
{
	[Export] public Node MapNode;

	Simulator simulator;

	TileMapLayer waterLayer;

	Vector2I WATER_CENTER = new Vector2I(5, 17);
	const int WATER_SOURCE_ID = 2;

	public override void _Ready()
	{
		simulator.Load(MapNode);
		waterLayer = MapNode.GetNode<TileMapLayer>("Level_0/Water");

		// Not needed for now.
		MapNode.GetNode<TileMapLayer>("Level_0/Water_Decoration").Visible = false;
	}

	void renderWater(Terrain terrain)
	{
		for (int y = 0; y < terrain.Rows - 1; y++)
		{
			for (int x = 0; x < terrain.Columns; x++)
			{
				var tile = terrain.Tiles[x, y];
				var coords = new Vector2I(x, y);
				if (tile.WaterShown > 0)
				{
					waterLayer.SetCell(coords, WATER_SOURCE_ID, WATER_CENTER);
				}
				else
				{
					waterLayer.SetCell(coords, -1, WATER_CENTER);
				}
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// TODO: This is all crap code that I added to debug the water sim.

		//simulator.Terrain.Tiles[9, 0].WaterHeight = 2;
		if (simulator.Tick % 20 == 0)
		{
			/*simulator.Terrain.Tiles[21, 20].WaterHeight = Math.Max(simulator.Terrain.Tiles[21, 20].WaterHeight, (byte)21);
			simulator.Terrain.Tiles[21, 20].WaterVelocity = WaterVelocity.Down;
			simulator.Terrain.Tiles[22, 20].WaterVelocity = WaterVelocity.Down;*/
			simulator.Terrain.Tiles[9, 2].WaterHeight = Math.Max(simulator.Terrain.Tiles[9, 2].WaterHeight, (byte)21);
		}
		if (Input.IsPhysicalKeyPressed(Key.A)) {
			Debug.WriteLine("Give us some water!");
			var bottomGround = MapNode.GetNode<TileMapLayer>("Level_0/Ground");
			var mapPos = bottomGround.LocalToMap(GetViewport().GetMousePosition());
			simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight = Math.Max(simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight, (byte)15);
		}
		if (Input.IsPhysicalKeyPressed(Key.R)) {
			for (int y = 0; y < simulator.Terrain.Rows; y++)
			{
				for (int x = 0; x < simulator.Terrain.Columns; x++)
				{
					simulator.Terrain.Tiles[x, y].WaterHeight = 0;
					simulator.Terrain.Tiles[x, y].WaterVelocity = WaterVelocity.None;
				}
			}
		}

		if (Input.IsPhysicalKeyPressed(Key.P)) {
			var bottomGround = MapNode.GetNode<TileMapLayer>("Level_0/Ground");
			var mapPos = bottomGround.LocalToMap(GetViewport().GetMousePosition());
			Debug.WriteLine(mapPos);
			simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight = (byte)Math.Min(simulator.Terrain.Tiles[mapPos.X, mapPos.Y].ObstructionHeight + 1, 4);

			var decorGround = MapNode.GetNode<TileMapLayer>("Level_0/Ground/Ground1");
			decorGround.SetCell(mapPos, 6, new Vector2I(14, 12));
		}

		if (Input.IsPhysicalKeyPressed(Key.D)) {
			var bottomGround = MapNode.GetNode<TileMapLayer>("Level_0/Ground");
			var mapPos = bottomGround.LocalToMap(GetViewport().GetMousePosition());
			Debug.WriteLine(mapPos);
			Debug.WriteLine("Water height: " + simulator.Terrain.Tiles[mapPos.X, mapPos.Y].WaterHeight);
			Debug.WriteLine("Terrain height: " + simulator.Terrain.Tiles[mapPos.X, mapPos.Y].GroundHeight);
		}

		//simulator.Terrain.Tiles[4, 4].WaterHeight = 2;
		simulator.Run();

		renderWater(simulator.Terrain);
	}
}
