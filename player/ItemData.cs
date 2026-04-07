using Godot;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public int itemId;
    [Export] public Texture2D icon;
}
