using Godot;
using System.Collections.Generic;

public partial class GlobalData : Node
{
	public Dictionary<string, ItemData> allItems = new();

	public override void _Ready()
	{
		LoadAllItems();
	}

	void LoadAllItems()
	{
		var dir = DirAccess.Open("res://Resources");
		if (dir == null)
			return;

		foreach (var file in dir.GetFiles())
		{
			if (!file.EndsWith(".tres") && !file.EndsWith(".res"))
				continue;

			var item = ResourceLoader.Load<ItemData>("res://Resources/" + file);
			if (item == null || item.itemRecipe == null || item.itemRecipe.Count == 0)
				continue;

			var exactKey = string.Join(",", item.itemRecipe);
			allItems[exactKey] = item;

			var compactRecipe = new List<string>();
			foreach (var itemId in item.itemRecipe)
			{
				if (!string.IsNullOrEmpty(itemId) && itemId != "null")
					compactRecipe.Add(itemId);
			}

			compactRecipe.Sort();
			if (compactRecipe.Count > 0)
				allItems[string.Join(",", compactRecipe)] = item;
		}
	}
}
