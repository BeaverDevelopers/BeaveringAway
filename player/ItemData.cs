using Godot;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public int ItemId;
    [Export] public Texture2D Icon;
    [Export] public int ItemCount = 1;
    [Export] public Godot.Collections.Array<string> itemRecipe = new();
}