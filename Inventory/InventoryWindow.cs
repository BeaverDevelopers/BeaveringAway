using Godot;
using Godot.Collections;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;


public partial class InventoryWindow : Control
{
	[Export] public InventoryDataNew InventoryData;
	[Export] public InventoryDataNew CraftingData;
	[Export] public InventoryDataNew ResultData;

	public Dictionary currentDraggedItem;

	public override void _Ready()
	{
		Visible = false;
		MouseFilter = MouseFilterEnum.Ignore;
		updateInventoryData();
		updateCraftingArea();
	}

	public override void _Process(double delta)
	{
		if (!HasNode("ItemDrag"))
			return;

		var itemDrag = GetNode<Control>("ItemDrag");
		itemDrag.Position = GetGlobalMousePosition() - itemDrag.Size / 2;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.Tab)
			{
				SetOpen(!Visible);
				GetViewport().SetInputAsHandled();
			}
			else if (key.Keycode == Key.Escape && Visible)
			{
				SetOpen(false);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	void SetOpen(bool open)
	{
		Visible = open;
		MouseFilter = MouseFilterEnum.Ignore;
		if (open)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void updateInventoryData()
	{
		var slotGroup = GetInventorySlotGroup();
		if (slotGroup == null || InventoryData == null)
			return;

		InventoryData.itemData ??= new Godot.Collections.Array<ItemData>();
		foreach (var slot in slotGroup.GetChildren())
			slot.QueueFree();

		foreach (var item in InventoryData.itemData)
		{
			var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
			newSlot.currentItem = item;
			slotGroup.AddChild(newSlot);
		}
	}

	public void updateCraftingArea()
	{
		var craftingSlotGroup = GetCraftingSlotGroup();
		if (craftingSlotGroup == null || CraftingData == null)
			return;

		var slotCount = Mathf.Max(CraftingData.itemData?.Count ?? 0, craftingSlotGroup.GetChildCount());
		EnsureItemDataSize(CraftingData, slotCount);

		foreach (var slot in craftingSlotGroup.GetChildren())
			slot.QueueFree();

		var currentItems = new Godot.Collections.Array<string>();
		foreach (var item in CraftingData.itemData)
		{
			var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
			newSlot.currentItem = item;
			craftingSlotGroup.AddChild(newSlot);
			currentItems.Add(item != null ? item.ItemId.ToString() : "null");
		}
		updateResultSlot(currentItems);
	}

	public void updateResultSlot(Godot.Collections.Array<string> currentRecipe)
	{
		var resultSlot = GetResultSlot();
		if (resultSlot == null || ResultData == null)
			return;

		EnsureItemDataSize(ResultData, 1);

		foreach (var slot in resultSlot.GetChildren())
			slot.QueueFree();

		var globalData = GetNodeOrNull<GlobalData>("/root/GlobalData");
		var allItems = globalData?.AllItemsRecipes;
		ResultData.itemData[0] = null;
		if (allItems != null)
		{
			foreach (var key in GetRecipeKeys(currentRecipe))
			{
				if (allItems.TryGetValue(key, out var result))
				{
					ResultData.itemData[0] = MakeStack(result, 1);
					break;
				}
			}
		}

		var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
		newSlot.currentItem = ResultData.itemData[0];
		resultSlot.AddChild(newSlot);
	}

	public void deleteCraftingItem()
	{
		if (CraftingData == null)
			return;

		CraftingData.itemData ??= new Godot.Collections.Array<ItemData>();
		for (var i = 0; i < CraftingData.itemData.Count; i++)
			CraftingData.itemData[i] = null;
	}

	public bool TryDropItemOnCraftingSlot(Control hoveredControl, ItemData item)
	{
		if (item == null || CraftingData == null)
			return false;

		var hoveredSlot = FindSlot(hoveredControl);
		var craftingSlotGroup = GetCraftingSlotGroup();
		if (hoveredSlot == null || craftingSlotGroup == null || hoveredSlot.GetParent() != craftingSlotGroup)
			return false;

		var targetIndex = hoveredSlot.GetIndex();
		EnsureItemDataSize(CraftingData, targetIndex + 1);
		var targetItem = CraftingData.itemData[targetIndex];
		if (targetItem == null)
		{
			CraftingData.itemData[targetIndex] = item;
			updateCraftingArea();
			return true;
		}

		if (!IsSameItem(targetItem, item))
			return false;

		targetItem.ItemCount = GetStackCount(targetItem) + GetStackCount(item);
		updateCraftingArea();
		return true;
	}

	public bool TryDropItemOnCraftingAtPosition(Vector2 globalPosition, ItemData item)
	{
		if (item == null || CraftingData == null)
			return false;

		var craftingSlotGroup = GetCraftingSlotGroup();
		if (craftingSlotGroup == null)
			return false;

		foreach (var child in craftingSlotGroup.GetChildren())
		{
			if (child is Slot slot && slot.GetGlobalRect().HasPoint(globalPosition))
				return TryDropItemOnCraftingSlot(slot, item);
		}

		return false;
	}

	bool inventoryMousePressed(InputEventMouseButton mouseEvent)
	{
        var hoveredNode = FindSlotAtPosition(GetGlobalMousePosition()) ?? FindSlot(GetViewport().GuiGetHoveredControl());
        if (hoveredNode == null)
            return false;

        var currentIndex = hoveredNode.GetIndex();
        var inventorySlotGroup = GetInventorySlotGroup();
        var craftingSlotGroup = GetCraftingSlotGroup();
        var resultSlot = GetResultSlot();

        if (inventorySlotGroup != null && hoveredNode.GetParent() == inventorySlotGroup)
        {
            if (!CreateDragItem(currentIndex, InventoryData, IsSplitModifierPressed(mouseEvent), "Inventory"))
                return false;

            updateInventoryData();
            return true;
        }

        if (craftingSlotGroup != null && hoveredNode.GetParent() == craftingSlotGroup)
        {
            if (!CreateDragItem(currentIndex, CraftingData, IsSplitModifierPressed(mouseEvent), "Crafting"))
                return false;

            updateCraftingArea();
            return true;
        }

        if (resultSlot != null && hoveredNode.GetParent() == resultSlot)
        {
            if (!CreateDragItem(0, ResultData, false, "Result", true))
                return false;
            return true;
        }

		return false;
    }

	bool inventoryMouseReleased(InputEventMouseButton mouseEvent)
	{
        if (HasNode("ItemDrag"))
            deleteDragedItem();

        if (currentDraggedItem == null || currentDraggedItem.Count == 0)
            return false;

        var item = (ItemData)currentDraggedItem["Item"];
        var index = (int)currentDraggedItem["Index"];
        var sourceInventory = (InventoryDataNew)currentDraggedItem["InventoryDataType"];
        var sourceName = (string)currentDraggedItem["SourceName"];
        var fromSplit = (bool)currentDraggedItem["FromSplit"];

        if (TryDropDraggedItemToHudInventory(item))
        {
            if (sourceName == "Result")
                ConsumeCraftingIngredients();

            ClearDraggedItem();
            updateInventoryData();
            updateCraftingArea();
            return true;
        }

        if (sourceName == "Result")
        {
            //trying to make placing dam count from inventory
            if (item.ItemId == 2)
            {
                var game = GetTree().CurrentScene as Game;
                for (int i = 0; i < item.ItemCount; i++)
                {
                    game.PlaceDam();
                    ConsumeCraftingIngredients();
                    ClearDraggedItem();
                }
                updateInventoryData();
                updateCraftingArea();
                return true;
            }
            //other non-dam items
            if (TryPlaceItemInWorld(item))
            {
                ConsumeCraftingIngredients();
                ClearDraggedItem();
                updateCraftingArea();
            }
            return true;
        }

        var hoveredNode = FindSlotAtPosition(GetGlobalMousePosition()) ?? FindSlot(GetViewport().GuiGetHoveredControl());
        if (hoveredNode == null)
        {
            //trying to make placing dam count from inventory
            if (item.ItemId == 2)
            {
                var game = GetTree().CurrentScene as Game;
                for (int i = 0; i < item.ItemCount; i++)
                {
                    game.PlaceDam();
                    ClearDraggedItem();
                }
                updateInventoryData();
                updateCraftingArea();
                return true;
            }
            //trying to place item in the world
            if (TryPlaceItemInWorld(item))
            {
                ClearDraggedItem();
                updateInventoryData();
                updateCraftingArea();
                return true;
            }
            RestoreDraggedItem(sourceInventory, index, item);
            return true;
        }

        var inventory = hoveredNode.GetParent();
        var slotGroup = GetInventorySlotGroup();
        var craftingSlotGroup = GetCraftingSlotGroup();
        var resultSlot = GetResultSlot();
        Debug.WriteLine(resultSlot);

        if (resultSlot != null && inventory == resultSlot)
        {
            RestoreDraggedItem(sourceInventory, index, item);
            return true;
        }

        if (slotGroup != null && inventory == slotGroup && InventoryData != null)
        {
            if (TryPlaceDraggedItem(InventoryData, hoveredNode.GetIndex(), item, sourceInventory, index, fromSplit))
            {
                ClearDraggedItem();
                updateInventoryData();
                updateCraftingArea();
            }
            else
            {
                RestoreDraggedItem(sourceInventory, index, item);
            }
            return true;
        }

        if (craftingSlotGroup != null && inventory == craftingSlotGroup && CraftingData != null)
        {
            if (TryPlaceDraggedItem(CraftingData, hoveredNode.GetIndex(), item, sourceInventory, index, fromSplit))
            {
                ClearDraggedItem();
                updateInventoryData();
                updateCraftingArea();
            }
            else
            {
                RestoreDraggedItem(sourceInventory, index, item);
            }
            return true;
        }

        RestoreDraggedItem(sourceInventory, index, item);
		return true;
    }

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				if (inventoryMousePressed(mouseEvent))
				{
					GetViewport().SetInputAsHandled();
				}
			}

			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
			{
                if (inventoryMouseReleased(mouseEvent))
                {
                    GetViewport().SetInputAsHandled();
                }
            }
		}
	}

	public bool createDragItem(int index, InventoryDataNew inventoryDataType)
	{
		return CreateDragItem(index, inventoryDataType, false, "Inventory");
	}

	public bool CreateDragItem(int index, InventoryDataNew inventoryDataType, bool splitOne, string sourceName, bool keepSource = false)
	{
		if (inventoryDataType == null)
			return false;

		EnsureItemDataSize(inventoryDataType, index + 1);
		var sourceItem = inventoryDataType.itemData[index];
		if (sourceItem == null)
			return false;

		var sourceCount = GetStackCount(sourceItem);
		var fromSplit = splitOne && sourceCount > 1;
		var draggedItem = fromSplit ? MakeStack(sourceItem, 1) : sourceItem;

		currentDraggedItem = new Dictionary
		{
			{ "InventoryDataType", inventoryDataType },
			{ "Item", draggedItem },
			{ "Index", index },
			{ "FromSplit", fromSplit },
			{ "SourceName", sourceName }
		};

		if (!keepSource)
		{
			if (fromSplit)
				sourceItem.ItemCount = sourceCount - 1;
			else
				inventoryDataType.itemData[index] = null;
		}

		var newDragItem = new TextureRect
		{
			Texture = draggedItem.Icon,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Name = "ItemDrag"
		};
		AddChild(newDragItem);
		return true;
	}

	public void deleteDragedItem()
	{
		GetNodeOrNull("ItemDrag")?.QueueFree();
	}

	void RestoreDraggedItem(InventoryDataNew sourceInventory, int index, ItemData item)
	{
		EnsureItemDataSize(sourceInventory, index + 1);
		if (sourceInventory != null)
		{
			var sourceItem = sourceInventory.itemData[index];
			if (sourceItem == null)
				sourceInventory.itemData[index] = item;
			else if (sourceItem != item && IsSameItem(sourceItem, item))
				sourceItem.ItemCount = GetStackCount(sourceItem) + GetStackCount(item);
		}
		ClearDraggedItem();
		updateInventoryData();
		updateCraftingArea();
	}

	bool TryDropDraggedItemToHudInventory(ItemData item)
	{
		var inventoryBar = FindVisibleInventoryBar(GetTree().Root);
		return inventoryBar != null && inventoryBar.TryDropItemAtPosition(GetGlobalMousePosition(), item);
	}

	bool TryPlaceDraggedItem(InventoryDataNew targetInventory, int targetIndex, ItemData item, InventoryDataNew sourceInventory, int sourceIndex, bool fromSplit)
	{
		if (targetInventory == null || item == null)
			return false;

		EnsureItemDataSize(targetInventory, targetIndex + 1);
		var targetItem = targetInventory.itemData[targetIndex];

		if (targetItem == null)
		{
			targetInventory.itemData[targetIndex] = item;
			return true;
		}

		if (IsSameItem(targetItem, item))
		{
			targetItem.ItemCount = GetStackCount(targetItem) + GetStackCount(item);
			return true;
		}

		if (fromSplit)
			return false;

		targetInventory.itemData[targetIndex] = item;
		EnsureItemDataSize(sourceInventory, sourceIndex + 1);
		if (sourceInventory != null)
			sourceInventory.itemData[sourceIndex] = targetItem;

		return true;
	}
	public bool TryPlaceItemInWorld(ItemData item)
	{

		var world = GetTree().CurrentScene.GetNode("world"); //get access to the world
		if (world == null)
		{
			Debug.WriteLine("No world");
			return false;
		}
		//getting access to the camera through world and player
		var player = world.GetNode("Player");
		if (player == null)
		{
			Debug.WriteLine("no player");
			return false;
		}
		var camera = player.GetNodeOrNull<Camera2D>("Camera2D");
		if (camera == null)
		{
			Debug.WriteLine("No camera");
			return false;
		}

		//getting the position of where to drop items through camera and mouse
		var mapPos = camera.GetGlobalMousePosition();

		//if the item is a hut it should only be placed in water
		if (item.ItemId == 3)
		{
			var game = GetTree().CurrentScene as Game;
			if(!game.IsPositionInWater(mapPos))
			{
				return false;
			}
				
		}
		
		for (int i = 0; i < item.ItemCount; i++)
		{
			var itemScene = item.ItemScene.Instantiate() as Node2D;
            if (itemScene is DroppedItem droppedItem)
            {
                droppedItem.ItemData = item.Duplicate() as ItemData;
            }
            world.AddChild(itemScene);
			itemScene.GlobalPosition = mapPos;
		}
		return true;
	
	}

	void ConsumeCraftingIngredients()
	{
		if (CraftingData == null)
			return;

		EnsureItemDataSize(CraftingData, 0);
		for (int i = 0; i < CraftingData.itemData.Count; i++)
		{
			var item = CraftingData.itemData[i];
			if (item == null)
				continue;

			item.ItemCount = GetStackCount(item) - 1;
			if (item.ItemCount <= 0)
				CraftingData.itemData[i] = null;
		}
	}

	void ClearDraggedItem()
	{
		currentDraggedItem = null;
	}

	void EnsureItemDataSize(InventoryDataNew data, int size)
	{
		if (data == null)
			return;

		data.itemData ??= new Godot.Collections.Array<ItemData>();
		while (data.itemData.Count < size)
			data.itemData.Add(null);
	}

	System.Collections.Generic.IEnumerable<string> GetRecipeKeys(Godot.Collections.Array<string> currentRecipe)
	{
		var exactKey = string.Join(",", currentRecipe);
		yield return exactKey;

		var compact = new System.Collections.Generic.List<string>();
		foreach (var itemId in currentRecipe)
		{
			if (!string.IsNullOrEmpty(itemId) && itemId != "null")
				compact.Add(itemId);
		}

		compact.Sort();
		if (compact.Count > 0)
			yield return string.Join(",", compact);
	}

	GridContainer GetInventorySlotGroup()
	{
		return GetNodeOrNull<GridContainer>("Inventory/MarginContainer/VBoxContainer/SlotGroup");
	}

	GridContainer GetCraftingSlotGroup()
	{
		return GetNodeOrNull<GridContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/CraftingSlotGroup");
	}

	PanelContainer GetResultSlot()
	{
		return GetNodeOrNull<PanelContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/ResultSlot");
	}

	Slot FindSlot(Control hovered)
	{
		while (hovered != null)
		{
			if (hovered is Slot slot)
				return slot;

			hovered = hovered.GetParent() as Control;
		}

		return null;
	}

	Slot FindSlotAtPosition(Vector2 globalPosition)
	{
		var craftingSlotGroup = GetCraftingSlotGroup();
		if (craftingSlotGroup != null)
		{
			foreach (var child in craftingSlotGroup.GetChildren())
			{
				if (child is Slot slot && slot.GetGlobalRect().HasPoint(globalPosition))
					return slot;
			}
		}

		var resultSlot = GetResultSlot();
		if (resultSlot != null)
		{
			foreach (var child in resultSlot.GetChildren())
			{
				if (child is Slot slot && slot.GetGlobalRect().HasPoint(globalPosition))
					return slot;
			}
		}

		var inventorySlotGroup = GetInventorySlotGroup();
		if (inventorySlotGroup != null)
		{
			foreach (var child in inventorySlotGroup.GetChildren())
			{
				if (child is Slot slot && slot.GetGlobalRect().HasPoint(globalPosition))
					return slot;
			}
		}

		return null;
	}

	InventoryBar FindVisibleInventoryBar(Node node)
	{
		if (node == null)
			return null;

		if (node is InventoryBar bar && bar.Visible)
			return bar;

		foreach (var child in node.GetChildren())
		{
			var found = FindVisibleInventoryBar(child);
			if (found != null)
				return found;
		}

		return null;
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

	static bool IsSplitModifierPressed(InputEventMouseButton mouseEvent)
	{
		return mouseEvent.CtrlPressed || mouseEvent.MetaPressed;
	}
}
