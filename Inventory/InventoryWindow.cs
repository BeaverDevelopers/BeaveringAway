using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Collections;


public partial class InventoryWindow : Control
{
	[Export] public InventoryDataNew InventoryData;
	[Export] public InventoryDataNew CraftingData;

	[Export] public InventoryDataNew ResultData;
	public Dictionary currentDraggedItem;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		updateInventoryData();
		//connectSignals();((
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!HasNode("ItemDrag"))
		{
			return;
		}

		var itemDrag = GetNode<Control>("ItemDrag");
		itemDrag.Position = GetGlobalMousePosition() - itemDrag.Size / 2;
		
	}

	//public void connectSignals(){var GlobalSignals = GetNode("/root/GlobalSignals");GlobalSignals.Connect("update_inventory_event_handler", (new Callable(this, nameof(updateInventoryData))));}
	
	public void updateInventoryData()
	{
		GD.Print("Updating Inventory UI...");
		var slotGrop = GetNode<GridContainer>("Inventory/MarginContainer/VBoxContainer/SlotGroup");
		foreach (var slot in slotGrop.GetChildren())
		{
			slot.QueueFree();
		}

		foreach (var item in InventoryData.itemData)
		{
			var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
			newSlot.currentItem = item;
			slotGrop.AddChild(newSlot);
			//newSlot.setItemSlot(); n
		}
		
	}

	public void updateCraftingArea()
	{
		var CraftingSlotGroup = GetNode<GridContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/CraftingSlotGroup");
		foreach (var slot in CraftingSlotGroup.GetChildren())
		{
			slot.QueueFree();
		}

		var currentItems = new Godot.Collections.Array<string>();
		foreach(var item in CraftingData.itemData)
		{
			var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
			newSlot.currentItem = item;
			CraftingSlotGroup.AddChild(newSlot);
			if (item != null)
			{
				currentItems.Append(item.ItemId.ToString());
			}
			else
			{
				currentItems.Append("null");
			}
			updateResultSlot(currentItems);
		}

	}

	public void updateResultSlot(Godot.Collections.Array<string> currentRecipe)
	{
		var ResultSlot = GetNode<PanelContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/ResultSlot");
		foreach (var slot in ResultSlot.GetChildren())
		{
			slot.QueueFree();
		}
		
		var allItems = GetNode<GlobalData>("/root/GlobalData").allItems;
		if (allItems.ContainsKey(currentRecipe))
		{
			var result = allItems[currentRecipe];
			ResultData.itemData[0] = (ItemData)result;
		}
		else
		{
			ResultData.itemData[0] = null;
		}
		var newSlot = GD.Load<PackedScene>("res://Inventory/Slot.tscn").Instantiate<Slot>();
		newSlot.currentItem = ResultData.itemData[0];
		ResultSlot.AddChild(newSlot);
	}

	public void deleteCraftingItem()
	{
		for (var index = 0; index < CraftingData.itemData.Count; index++)
		{
			CraftingData.itemData[index] = null;
		}
	}

	public override void _Input(InputEvent @event)
	{
		//var GlobalSignals = GetNode("/root/GlobalSignals");

		if (@event is InputEventMouseButton mouseEvent)
		{
			
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				var hoveredNode = GetViewport().GuiGetHoveredControl();

				if (hoveredNode is Slot)
				{
					var currentIndex = hoveredNode.GetIndex();
					//Checks if it is an item from the inventory
					if (hoveredNode.GetParent().Name == "SlotGroup") 
					{
						if  (InventoryData.itemData[currentIndex] == null)
						{
							return;
						}
						createDragItem(currentIndex, InventoryData);
						InventoryData.itemData[currentIndex] = null;
						//GlobalSignals.EmitSignal("update_inventory_event_handler");
						updateInventoryData();
						return;
					}
				
					//Checks if it is a crafting item
					if (hoveredNode.GetParent().Name == "CraftingSlotGroup") 
					{
						if  (CraftingData.itemData[currentIndex] == null)
						{
							return;
						}
						createDragItem(currentIndex, CraftingData);
						CraftingData.itemData[currentIndex] = null;
						//GlobalSignals.EmitSignal("update_inventory_event_handler");
						updateCraftingArea();
						return;
					}

					//Checks if it is a result item
					if (hoveredNode.GetParent().Name == "ResultSlot") 
					{
						if  (ResultData.itemData[0] == null)
						{
							return;
						}
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
				{
					deleteDragedItem();
				}

				if (currentDraggedItem == null) // if no item is being dragged, return
				{
					return;
				}

				var hoveredNode = GetViewport().GuiGetHoveredControl();
				var inventory = hoveredNode.GetParent();
				var item = (ItemData)currentDraggedItem["Item"];
				var index = (int)currentDraggedItem["Index"];
				var draggedItemFromInventory = (InventoryDataNew)currentDraggedItem["InventoryDataType"]; //for failed drop



				if (hoveredNode is Slot == false) // if mouse is hovering is hovering a slot, it jumps back to where it comes from
				{
					draggedItemFromInventory.itemData[index] = null;
					currentDraggedItem.Clear();
					updateInventoryData();
					updateCraftingArea();
					updateResultSlot(item.itemRecipe);
					return;
				}

				// if the slot is not empty, it jumps back to where it comes from

				var SlotGroup = GetNode<GridContainer>("Inventory/MarginContainer/VBoxContainer/SlotGroup");
				if (inventory == SlotGroup && InventoryData.itemData[hoveredNode.GetIndex()] != null) // if the slot not empty
				{
					draggedItemFromInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateInventoryData();
					return;
				}
					
				var CraftingSlotGroup = GetNode<GridContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/CraftingSlotGroup");
				if (inventory == CraftingSlotGroup && CraftingData.itemData[hoveredNode.GetIndex()] != null) // if the slot not empty
				{
					draggedItemFromInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateCraftingArea();
					return;
				}
				
				if (inventory == SlotGroup)
				{
					InventoryData.itemData[hoveredNode.GetIndex()] = item; // if the slot is empty, move the item to the new slot
					currentDraggedItem.Clear();
					updateInventoryData();
					return;
				}

				var ResultSlot = GetNode<PanelContainer>("Crafting/MarginContainer/VBoxContainer/HBoxContainer/ResultSlot");	
				if (inventory == ResultSlot)
				{
					draggedItemFromInventory.itemData[index] = item;
					currentDraggedItem.Clear();
					updateCraftingArea();
					updateInventoryData();
				}

				if (inventory == CraftingSlotGroup)
				{
					CraftingData.itemData[hoveredNode.GetIndex()] = item; // if the slot is empty, move the item to the new slot
					currentDraggedItem.Clear();
					updateCraftingArea();
					return;
				}


		

				//InventoryData.itemData[hoveredNode.GetIndex()] = item; // if the slot is empty, move the item to the new slot
				//currentDraggedItem=null;
				//updateInventoryData();
			}
				
		}
	
	}
	public void createDragItem(int index, InventoryDataNew InventoryDataType) // InventoryDataType can be for the inventory, crafting area or result slot
	{
		currentDraggedItem = new Dictionary()
		{
			{"InventoryDataType", InventoryDataType},
			{"Item", InventoryData.itemData[index]},
			{"Index", index}
		};
		TextureRect newDragItem = new TextureRect();
		newDragItem.Texture = InventoryDataType.itemData[index].Icon;
		newDragItem.MouseFilter = Control.MouseFilterEnum.Ignore; // ignore mouse event for the drag item
		newDragItem.Name = "ItemDrag";
		AddChild(newDragItem);

		
	}

	public void deleteDragedItem()
	{
		GetNode("ItemDrag").QueueFree();
	}
}
