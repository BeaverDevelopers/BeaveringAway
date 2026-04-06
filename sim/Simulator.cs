using Godot;

public struct Simulator
{
	public Terrain Terrain;
	public int Tick;

	public void Load(Node mapNode)
	{
		Terrain = new Terrain();
		Terrain.LoadTerrain(mapNode);
		//Debugger.Launch();
	}

	public void Run()
	{
		Tick++;
		WaterSimulation.Run(Terrain, Tick);
	}
}
