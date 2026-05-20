using Godot;
using System.Collections.Generic;

public partial class GlobalData : Node
{
	public Dictionary<string, ItemData> AllItemsRecipes = new();
	public Dictionary<int, ItemData> ItemByID = [];

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
			if (item == null || item.itemRecipe == null)
				continue;

			ItemByID.Add(item.ItemId, item);

			if (item.itemRecipe.Count == 0)
				continue;

			var exactKey = string.Join(",", item.itemRecipe);
			AllItemsRecipes[exactKey] = item;

			var compactRecipe = new List<string>();
			foreach (var itemId in item.itemRecipe)
			{
				if (!string.IsNullOrEmpty(itemId) && itemId != "null")
					compactRecipe.Add(itemId);
			}

			compactRecipe.Sort();
			if (compactRecipe.Count > 0)
				AllItemsRecipes[string.Join(",", compactRecipe)] = item;
		}
	}
}
