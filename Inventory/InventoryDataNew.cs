using Godot;

[GlobalClass]
public partial class InventoryDataNew : Resource
{
	[Export] public Godot.Collections.Array<ItemData> itemData = new();

	public bool TryAddItem(ItemData item, int amount = 1)
	{
		if (item == null || amount == 0)
			return false;

		if (amount < 0)
			return TryRemoveItem(item.ItemId, -amount);

		EnsureItems();

		for (int i = 0; i < itemData.Count; i++)
		{
			if (IsSameItem(itemData[i], item))
			{
				itemData[i].ItemCount = GetStackCount(itemData[i]) + amount;
				return true;
			}
		}

		for (int i = 0; i < itemData.Count; i++)
		{
			if (itemData[i] == null)
			{
				itemData[i] = MakeStack(item, amount);
				return true;
			}
		}

		return false;
	}

	public bool TryRemoveItem(int itemId, int amount = 1)
	{
		if (amount <= 0)
			return false;

		EnsureItems();
		if (GetItemCount(itemId) < amount)
			return false;

		var remaining = amount;
		for (int i = itemData.Count - 1; i >= 0 && remaining > 0; i--)
		{
			var item = itemData[i];
			if (item == null || item.ItemId != itemId)
				continue;

			var removed = Mathf.Min(GetStackCount(item), remaining);
			item.ItemCount = GetStackCount(item) - removed;
			remaining -= removed;

			if (item.ItemCount <= 0)
				itemData[i] = null;
		}

		return true;
	}

	public int GetItemCount(int itemId)
	{
		EnsureItems();

		var count = 0;
		foreach (var item in itemData)
		{
			if (item != null && item.ItemId == itemId)
				count += GetStackCount(item);
		}

		return count;
	}

	public void ClearItems()
	{
		EnsureItems();
		for (int i = 0; i < itemData.Count; i++)
			itemData[i] = null;
	}

	void EnsureItems()
	{
		itemData ??= new Godot.Collections.Array<ItemData>();
	}

	static bool IsSameItem(ItemData a, ItemData b)
	{
		return a != null && b != null && a.ItemId == b.ItemId;
	}

	static int GetStackCount(ItemData item)
	{
		return item?.ItemCount > 0 ? item.ItemCount : 1;
	}

	static ItemData MakeStack(ItemData source, int count)
	{
		var stack = source.Duplicate() as ItemData ?? new ItemData
		{
			ItemId = source.ItemId,
			Icon = source.Icon,
			itemRecipe = source.itemRecipe
		};
		stack.ItemCount = count;
		return stack;
	}
}
