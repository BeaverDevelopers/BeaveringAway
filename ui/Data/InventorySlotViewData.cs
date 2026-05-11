using Godot;

public class InventorySlotViewData
{
	public string ItemId = "";
	public string DisplayName = "";
	public int Count;
	public Texture2D Icon;
	public ItemData Item;
	public bool IsSelected;
	public bool IsLocked;

	public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;

	public InventorySlotViewData()
	{
	}

	public InventorySlotViewData(string itemId, string displayName, int count, Texture2D icon = null, bool isSelected = false, ItemData item = null)
	{
		ItemId = itemId;
		DisplayName = displayName;
		Count = count;
		Icon = icon;
		IsSelected = isSelected;
		Item = item;
	}

	public InventorySlotViewData Copy()
	{
		return new InventorySlotViewData
		{
			ItemId = ItemId,
			DisplayName = DisplayName,
			Count = Count,
			Icon = Icon,
			Item = Item,
			IsSelected = IsSelected,
			IsLocked = IsLocked
		};
	}

	public static InventorySlotViewData FromItem(ItemData item, bool isSelected = false, string displayName = "")
	{
		if (item == null)
			return Empty();

		var count = item.ItemCount > 0 ? item.ItemCount : 1;
		return new InventorySlotViewData(
			item.ItemId.ToString(),
			displayName,
			count,
			item.Icon,
			isSelected,
			item);
	}

	public static InventorySlotViewData Empty()
	{
		return new InventorySlotViewData();
	}
}
