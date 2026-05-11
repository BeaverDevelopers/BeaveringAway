using Godot;
using System.Collections.Generic;

public partial class InventoryData : Node
{
	const string ActiveInventoryPath = "res://Inventory/TestInventory.tres";

	public static int ItemId { get; set; } = -1; // -1 = 空
	public static int Count { get; set; } = 0;

	static readonly Dictionary<int, string> KnownItemPaths = new()
	{
		{ 1, "res://Resources/logs.tres" }
	};

	public static bool AddItem(int id, int amount = 1)
	{
		if (amount == 0)
			return false;

		var activeInventory = GetActiveInventory();
		if (activeInventory != null)
		{
			var changed = amount > 0
				? activeInventory.TryAddItem(LoadItemTemplate(id), amount)
				: activeInventory.TryRemoveItem(id, -amount);

			SyncLegacyCount(activeInventory, id);
			return changed;
		}

		return AddToLegacySlot(id, amount);
	}

	public static void Clear()
	{
		GetActiveInventory()?.ClearItems();
		ItemId = -1;
		Count = 0;
	}

	static bool AddToLegacySlot(int id, int amount)
	{
		if (amount > 0)
		{
			if (Count == 0)
			{
				ItemId = id;
				Count = amount;
				return true;
			}

			if (ItemId != id)
				return false;

			Count += amount;
			return true;
		}

		if (ItemId != id || Count < -amount)
			return false;

		Count += amount;
		if (Count == 0)
		{
			ItemId = -1;
		}

		return true;
	}

	static InventoryDataNew GetActiveInventory()
	{
		return ResourceLoader.Load<InventoryDataNew>(ActiveInventoryPath);
	}

	static ItemData LoadItemTemplate(int id)
	{
		if (KnownItemPaths.TryGetValue(id, out var knownPath))
		{
			var knownItem = ResourceLoader.Load<ItemData>(knownPath);
			if (knownItem != null)
				return knownItem;
		}

		var dir = DirAccess.Open("res://Resources");
		if (dir == null)
			return null;

		foreach (var file in dir.GetFiles())
		{
			if (!file.EndsWith(".tres") && !file.EndsWith(".res"))
				continue;

			var item = ResourceLoader.Load<ItemData>("res://Resources/" + file);
			if (item != null && item.ItemId == id)
				return item;
		}

		return null;
	}

	static void SyncLegacyCount(InventoryDataNew inventory, int id)
	{
		Count = inventory.GetItemCount(id);
		ItemId = Count > 0 ? id : -1;
	}
}
