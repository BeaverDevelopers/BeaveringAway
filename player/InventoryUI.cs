using Godot;

public partial class InventoryUI : CanvasLayer
{
	[Export] public Texture2D emptyIcon;
	[Export] public Godot.Collections.Array<ItemData> itemDatas;

	[Export] public TextureRect itemIcon;
	[Export] public Label itemIdLabel;
	[Export] public Label countLabel;

	// 每帧刷新UI + 检测按键（最简单、最稳定）
	public override void _Process(double delta)
	{


		// 刷新UI
		UpdateUI();
	}
	public override void _Input(InputEvent @event)
	{
		// 仅处理键盘事件
		if (@event is InputEventKey keyEvent)
		{
			// 按键 1 按下 → 添加物品
			if (keyEvent.Keycode == Key.Key1 && keyEvent.Pressed)
			{
				GD.Print("✅ 按键1 触发！添加物品 ID=1");
				InventoryData.AddItem(1);
			}
			
			if (keyEvent.Keycode == Key.E && keyEvent.Pressed)
			{
				GD.Print("✅ Chopped A tree ID=2");
				InventoryData.AddItem(1, 3);
			}

			// 按键 C 按下 → 清空背包
			if (keyEvent.Keycode == Key.C && keyEvent.Pressed)
			{
				GD.Print("✅ 按键C 触发！清空背包");
				InventoryData.Clear();
			}
		}
	}

	private void UpdateUI()
	{
		// 严格匹配你的单格子空状态
		if (InventoryData.ItemId == -1)
		{
			itemIcon.Texture = emptyIcon;
			itemIdLabel.Text = "null";
			countLabel.Text = "0";
			return;
		}

		// 查找对应物品
		ItemData target = null;
		foreach (var data in itemDatas)
		{
			if (data != null && data.ItemId == InventoryData.ItemId)
			{
				target = data;
				break;
			}
		}

		// 更新显示
		itemIcon.Texture = target?.Icon ?? emptyIcon;
		itemIdLabel.Text = $"item {InventoryData.ItemId}";
		countLabel.Text = InventoryData.Count.ToString();
	}
}
