using System.ComponentModel;
using Godot;
using Godot.Collections;


public partial class InventoryWindow : Control
{
	[Export] public InventoryDataNew InventoryData;
	public Dictionary currentDraggedItem;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		updateInventoryData();
		//connectSignals();((
		updateInventoryData();
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
			newSlot.setItemSlot();
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
				GD.Print(hoveredNode);
				if (hoveredNode is Slot)
				{
					GD.Print("It's slot");
					var currentIndex = hoveredNode.GetIndex();
					if  (InventoryData.itemData[currentIndex] == null)
					{
						return;
					}
					createDragItem(currentIndex);
					InventoryData.itemData[currentIndex] = null;
					//GlobalSignals.EmitSignal("update_inventory_event_handler");
					updateInventoryData();
				}
			}

			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
			{

				if (currentDraggedItem == null) // if no item is being dragged, return
				{
					return;
				}

				var hoveredNode = GetViewport().GuiGetHoveredControl();
				var item = (ItemData)currentDraggedItem["Item"];
				var index = (int)currentDraggedItem["Index"];

				

				if (HasNode("ItemDrag"))
				{
					deleteDragedItem();
				}


				if (hoveredNode is Slot == false) // if mouse is hovering is hovering a slot, it jumps back to where it comes from
				{
					InventoryData.itemData[index] = item;
					//GlobalSignals.EmitSignal("update_inventory_event_handler");
					updateInventoryData();
					return;
				}

				if (InventoryData.itemData[hoveredNode.GetIndex()] != null) // if the slot not empty
				{
					InventoryData.itemData[index] = item;
					//GlobalSignals.EmitSignal("update_inventory_event_handler");
					updateInventoryData();
					return;
				}

		

				InventoryData.itemData[hoveredNode.GetIndex()] = item; // if the slot is empty, move the item to the new slot
				currentDraggedItem=null;
				//GlobalSignals.EmitSignal("update_inventory_event_handler");
				updateInventoryData();
			}
				
		}
	}

	public void createDragItem(int index)
	{
		currentDraggedItem = new Dictionary()
		{
			{"Item", InventoryData.itemData[index]},
			{"Index", index}
		};
		TextureRect newDragItem = new TextureRect();
		newDragItem.Texture = InventoryData.itemData[index].Icon;
		newDragItem.MouseFilter = Control.MouseFilterEnum.Ignore; // ignore mouse event for the drag item
		newDragItem.Name = "ItemDrag";
		AddChild(newDragItem);

		
	}

	public void deleteDragedItem()
	{
		GetNode("ItemDrag").QueueFree();
	}
}
