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
	public Sprite2D Sprite;
}
