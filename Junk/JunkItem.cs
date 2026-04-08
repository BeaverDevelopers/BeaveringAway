using Godot;

public enum JunkType
{
	Log,
	Leaf,
	Bottle,
	Fish
}

public class JunkItem
{
	public JunkType Type;
	public Vector2 WorldPos;
	public Vector2 Velocity;
	public Node2D Node;
	public int SpawnTick;
	public int SlideBias; // -1 left, +1 right, 0 no preference
}
