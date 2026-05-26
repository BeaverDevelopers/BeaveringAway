using Godot;
using System.Collections.Generic;
using System.IO;

public readonly struct StolenItemResult
{
	public int ItemId { get; init; }
	public Texture2D Icon { get; init; }
	public string DisplayName { get; init; }
}

public partial class InventoryData : Node
{
	const string ActiveInventoryPath = "res://Inventory/MainInventory.tres";

	public const int LogItemId = 1;

	public static int ItemId { get; set; } = -1; // -1 = 空
	public static int Count { get; set; } = 0;

	static readonly Dictionary<int, string> KnownItemPaths = new()
	{
		{ LogItemId, "res://Resources/logs.tres" }
	};

	public static bool HasLogs() => HasItem(LogItemId);

	public static bool HasAnyItems()
	{
		var activeInventory = GetActiveInventory();
		if (activeInventory != null)
		{
			activeInventory.PurgeEmptyStacks();
			foreach (var item in activeInventory.itemData)
			{
				if (item != null && item.ItemCount > 0)
					return true;
			}

			return false;
		}

		return ItemId >= 0 && Count > 0;
	}

	public static bool TryStealRandomItem(out StolenItemResult stolen)
	{
		stolen = default;
		var activeInventory = GetActiveInventory();
		if (activeInventory != null)
			return TryStealRandomFromInventory(activeInventory, out stolen);

		if (ItemId < 0 || Count <= 0)
			return false;

		int stolenId = ItemId;
		var legacyItem = LoadItemTemplate(stolenId);
		var icon = legacyItem?.Icon;
		var displayName = GetItemDisplayName(legacyItem);
		if (!AddItem(stolenId, -1))
			return false;

		stolen = new StolenItemResult
		{
			ItemId = stolenId,
			Icon = icon,
			DisplayName = displayName
		};
		return true;
	}

	static bool TryStealRandomFromInventory(InventoryDataNew inventory, out StolenItemResult stolen)
	{
		stolen = default;
		inventory.PurgeEmptyStacks();

		var occupiedSlots = new List<int>();
		for (int i = 0; i < inventory.itemData.Count; i++)
		{
			var item = inventory.itemData[i];
			if (item != null && item.ItemCount > 0)
				occupiedSlots.Add(i);
		}

		if (occupiedSlots.Count == 0)
			return false;

		var rng = new RandomNumberGenerator();
		int slotIndex = occupiedSlots[rng.RandiRange(0, occupiedSlots.Count - 1)];
		var stack = inventory.itemData[slotIndex];
		int itemId = stack.ItemId;
		var icon = stack.Icon;
		var displayName = GetItemDisplayName(stack);

		if (!inventory.TryRemoveItem(itemId, 1))
			return false;

		SyncLegacyCount(inventory, itemId);
		stolen = new StolenItemResult
		{
			ItemId = itemId,
			Icon = icon,
			DisplayName = displayName
		};
		return true;
	}

	static string GetItemDisplayName(ItemData item)
	{
		if (item == null)
			return "an item";

		if (!string.IsNullOrEmpty(item.ResourcePath))
			return Path.GetFileNameWithoutExtension(item.ResourcePath);

		return $"item {item.ItemId}";
	}

	public static bool HasItem(int id)
	{
		var activeInventory = GetActiveInventory();
		if (activeInventory != null)
			return activeInventory.GetItemCount(id) > 0;

		return ItemId == id && Count > 0;
	}

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
