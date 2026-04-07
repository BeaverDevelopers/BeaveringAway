using Godot;

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
            itemIdLabel.Text = "null";
            countLabel.Text = "0";
            return;
        }

        ItemData targetData = null;
        foreach (var data in itemDatas)
        {
            if (data != null && data.itemId == InventoryData.ItemId)
            {
                targetData = data;
                break;
            }
        }

        itemIcon.Texture = targetData?.icon ?? emptyIcon;
        itemIdLabel.Text = targetData != null ? $"item {InventoryData.ItemId}" : "null";
        countLabel.Text = InventoryData.Count.ToString();
    }
}