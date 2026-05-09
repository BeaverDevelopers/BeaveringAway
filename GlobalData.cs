using Godot;
using Godot.Collections;
using System;
using System.ComponentModel;

public partial class GlobalData : Node
{
	[Export] public Dictionary allItems = new Dictionary();
	public override void _Ready()
	{
		getAllItems();
	}

	public void getAllItems()
	{
		
		foreach (var item in DirAccess.GetFilesAt("res://Resources/"))
			{
				var currentItem = ResourceLoader.Load<ItemData>("res://Resources/" + item); //is this correct?
				if (currentItem == null) // if the file is not an ItemData resource, skip it
				{
					GD.Print("File " + item + " is not an ItemData resource, skipping.");
					continue;
				}
				if (currentItem.itemRecipe.Count == 0) //if the item is not a recipe we skip it
				{
					continue;
				}
				allItems[string.Join(",", currentItem.itemRecipe)] = currentItem; // add the item to the dictionary with the recipe as key

			}
	}
}
