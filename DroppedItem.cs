using Godot;
using System;

public partial class DroppedItem : Node2D
{
    public ItemData ItemData;
    private Area2D _interactable;
    private Sprite2D _sprite;
 
    public override void _Ready()
    {
        _interactable = GetNode<Area2D>("Log/Interactable");
        _sprite = GetNode<Sprite2D>("Log");
        if (ItemData == null)
        {
            ItemData = new ItemData();
        }
        _sprite.Texture = ItemData.Icon;
        Callable interractCallable = new Callable(this, MethodName.OnInteract);
        _interactable.Set("interact", interractCallable);
    }

    void OnInteract()
    {
        if (InventoryData.AddItem(ItemData.ItemId, 1))
        {
            GD.Print("Beaver picked up an item");
            QueueFree();
        }
        else
        {
            GD.Print("Failed to pick up an item");
        }
    }
}
