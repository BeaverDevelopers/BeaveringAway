using System.Linq;
using Godot;
using Godot.Collections;


public partial class InventoryWindow : Control
{
	[Export] public InventoryDataNew InventoryData;
	[Export] public InventoryDataNew CraftingData;
	[Export] public InventoryDataNew ResultData;

	public Dictionary currentDraggedItem;

	public override void _Ready()
	{
		Visible = false;
		updateInventoryData();
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
		if (open)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void updateInventoryData()
	{
		var slotGroup = GetNode<GridContainer>("Inventory/MarginContainer/VBoxContainer/SlotGroup");
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
		var craftingSlotGroup = GetNode<GridContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/CraftingSlotGroup");
		foreach (var slot in craftingSlotGroup.GetChildren())
			slot.QueueFree();

		var currentItems = new Godot.Collections.Array<string>();
		foreach (var item in CraftingData.itemData)
		{
			var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
			newSlot.currentItem = item;
			craftingSlotGroup.AddChild(newSlot);
			currentItems.Append(item != null ? item.ItemId.ToString() : "null");
		}
		updateResultSlot(currentItems);
	}

	public void updateResultSlot(Godot.Collections.Array<string> currentRecipe)
	{
		var resultSlot = GetNode<PanelContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/ResultSlot");
		foreach (var slot in resultSlot.GetChildren())
			slot.QueueFree();

		var recipeKey = string.Join(",", currentRecipe);
		var allItems = GetNode<GlobalData>("/root/GlobalData").allItems;
		ResultData.itemData[0] = allItems.ContainsKey(recipeKey) ? (ItemData)allItems[recipeKey] : null;

		var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
		newSlot.currentItem = ResultData.itemData[0];
		resultSlot.AddChild(newSlot);
	}

	public void deleteCraftingItem()
	{
		for (var i = 0; i < CraftingData.itemData.Count; i++)
			CraftingData.itemData[i] = null;
	}

	public override void _Input(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				var hoveredNode = GetViewport().GuiGetHoveredControl();
				if (hoveredNode is Slot)
				{
					var currentIndex = hoveredNode.GetIndex();

					if (hoveredNode.GetParent().Name == "SlotGroup")
					{
						if (InventoryData.itemData[currentIndex] == null) return;
						createDragItem(currentIndex, InventoryData);
						InventoryData.itemData[currentIndex] = null;
						updateInventoryData();
						return;
					}

					if (hoveredNode.GetParent().Name == "CraftingSlotGroup")
					{
						if (CraftingData.itemData[currentIndex] == null) return;
						createDragItem(currentIndex, CraftingData);
						CraftingData.itemData[currentIndex] = null;
						updateCraftingArea();
						return;
					}

					if (hoveredNode.GetParent().Name == "ResultSlot")
					{
						if (ResultData.itemData[0] == null) return;
						createDragItem(0, ResultData);
						ResultData.itemData[0] = null;
						deleteCraftingItem();
						updateCraftingArea();
						updateResultSlot([]);
						return;
					}
				}
			}

			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
			{
				if (HasNode("ItemDrag"))
					deleteDragedItem();

				if (currentDraggedItem == null)
					return;

				var hoveredNode = GetViewport().GuiGetHoveredControl();
				var inventory = hoveredNode?.GetParent();
				var item = (ItemData)currentDraggedItem["Item"];
				var index = (int)currentDraggedItem["Index"];
				var sourceInventory = (InventoryDataNew)currentDraggedItem["InventoryDataType"];

				if (hoveredNode is not Slot)
				{
					// Failed drop — restore item to source
					sourceInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateInventoryData();
					updateCraftingArea();
					return;
				}

				var slotGroup = GetNode<GridContainer>("Inventory/MarginContainer/VBoxContainer/SlotGroup");
				var craftingSlotGroup = GetNode<GridContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/CraftingSlotGroup");
				var resultSlot = GetNode<PanelContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/ResultSlot");

				// Bounce back if target slot occupied
				if (inventory == slotGroup && InventoryData.itemData[hoveredNode.GetIndex()] != null)
				{
					sourceInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateInventoryData();
					return;
				}

				if (inventory == craftingSlotGroup && CraftingData.itemData[hoveredNode.GetIndex()] != null)
				{
					sourceInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateCraftingArea();
					return;
				}

				// Can't drop into result slot
				if (inventory == resultSlot)
				{
					sourceInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateCraftingArea();
					updateInventoryData();
					return;
				}

				if (inventory == slotGroup)
				{
					InventoryData.itemData[hoveredNode.GetIndex()] = item;
					currentDraggedItem.Clear();
					updateInventoryData();
					return;
				}

				if (inventory == craftingSlotGroup)
				{
					CraftingData.itemData[hoveredNode.GetIndex()] = item;
					currentDraggedItem.Clear();
					updateCraftingArea();
					return;
				}

				// Unrecognised drop target — restore
				sourceInventory.itemData[index] = item;
				currentDraggedItem.Clear();
				updateInventoryData();
				updateCraftingArea();
			}
		}
	}

	public void createDragItem(int index, InventoryDataNew inventoryDataType)
	{
		currentDraggedItem = new Dictionary
		{
			{ "InventoryDataType", inventoryDataType },
			{ "Item", inventoryDataType.itemData[index] },
			{ "Index", index }
		};
		var newDragItem = new TextureRect
		{
			Texture = inventoryDataType.itemData[index].Icon,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Name = "ItemDrag"
		};
		AddChild(newDragItem);
	}

	public void deleteDragedItem()
	{
		GetNode("ItemDrag").QueueFree();
	}
}
