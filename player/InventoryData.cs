using Godot;
using System;

public partial class InventoryData : Node
{
    public static int ItemId { get; set; } = -1; // -1 = 空
    public static int Count { get; set; } = 0;

    // add items
    public static void AddItem(int id, int amount = 1)
    {

        if (Count == 0)
        {
            ItemId = id;
            Count = amount;
        }
        else if (ItemId == id)
        {
            Count += amount;
        }
    }

    // clear inventory
    public static void Clear()
    {
        ItemId = -1;
        Count = 0;
    }
}
