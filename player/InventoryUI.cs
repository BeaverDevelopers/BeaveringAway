using Godot;
using System.Linq;

public partial class InventoryUI : CanvasLayer
{
	[Export] public Texture2D emptyIcon;
    [Export] public ItemData[] itemDatas;

    [Export] public TextureRect itemIcon;
	[Export] public Label itemIdLabel;
	[Export] public Label countLabel;

	public override void _Process(double delta)
	{
		UpdateUI();
	}

	void UpdateUI()
	{
		// 1. 背包为空 → 直接显示空状态，退出方法
		if (InventoryData.Count <= 0)
		{
			itemIcon.Texture = emptyIcon;
			itemIdLabel.Text = "null";
			countLabel.Text = "0";
			return; // 关键：阻止后面的代码覆盖空状态
		}

		// 2. 背包有物品 → 查找对应数据
		ItemData targetData = null;
		foreach (var data in itemDatas)
		{
			if (data.itemId == InventoryData.ItemId)
			{
				targetData = data;
				break;
			}
		}

		// 3. 更新UI（空合并运算符：找不到物品就显示空图标）
		itemIcon.Texture = targetData?.icon ?? emptyIcon;
		itemIdLabel.Text = targetData != null ? $"item {InventoryData.ItemId}" : "null";
		countLabel.Text = InventoryData.Count.ToString();
	}
}
