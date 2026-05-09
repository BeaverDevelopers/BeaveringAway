using Godot;

[GlobalClass]

public partial class InventoryDataNew : Resource
{
    [Export] public Godot.Collections.Array<ItemData> itemData;
    
}