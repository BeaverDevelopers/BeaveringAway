using Godot;
using System.Linq;

public partial class InventoryUI : CanvasLayer
{
    [Export] public Texture2D emptyIcon;
    [Export] public Godot.Collections.Array<ItemData> itemDatas;

    [Export] public TextureRect itemIcon;
    [Export] public Label itemIdLabel;
    [Export] public Label countLabel;

    public override void _Process(double delta)
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (InventoryData.Count <= 0)
        {
            itemIcon.Texture = emptyIcon;
            itemIdLabel.Text = "空";
            countLabel.Text = "0";
        }
        else
        {
            var data = itemDatas.FirstOrDefault(i => i.itemId == InventoryData.ItemId);
            itemIcon.Texture = data?.icon ?? emptyIcon;
            itemIdLabel.Text = $"物品 {InventoryData.ItemId}";
            countLabel.Text = InventoryData.Count.ToString();
        }
    }
}