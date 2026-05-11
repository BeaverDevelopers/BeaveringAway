using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class LegacyInventoryAdapter : Node
{
	[Export] public bool PublishOnReady = true;
	[Export] public string LogItemPath = "res://Resources/logs.tres";
	[Export] public string LogIconPath = "res://Junk/sprites/Log.png";

	ItemData _logItem;
	Texture2D _logIcon;
	int _lastItemId = int.MinValue;
	int _lastCount = int.MinValue;

	public override void _Ready()
	{
		_logItem = GD.Load<ItemData>(LogItemPath);
		_logIcon = _logItem?.Icon ?? GD.Load<Texture2D>(LogIconPath);
		if (PublishOnReady)
			Publish();
	}

	public override void _Process(double delta)
	{
		if (_lastItemId == InventoryData.ItemId && _lastCount == InventoryData.Count)
			return;

		Publish();
	}

	void Publish()
	{
		_lastItemId = InventoryData.ItemId;
		_lastCount = InventoryData.Count;

		var slots = new List<InventorySlotViewData>();

		if (InventoryData.ItemId == 1 && InventoryData.Count > 0)
		{
			var item = _logItem?.Duplicate() as ItemData ?? new ItemData
			{
				ItemId = 1,
				Icon = _logIcon
			};
			item.ItemCount = InventoryData.Count;
			slots.Add(InventorySlotViewData.FromItem(item, true, "Log"));
		}

		UIEventBus.SetInventory(slots);
	}
}
