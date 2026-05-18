using System.Collections.Generic;
using System.Diagnostics;
using Godot;

[GlobalClass]
public partial class InventoryBar : PanelContainer
{
	[Export] public int SlotCount = 5;
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle;
	[Export] public bool AllowDragAndDrop = true;
	[Export] public bool AcceptEventBusInventory;
	[Export] public Vector2 DragPreviewSize = new(32, 32);
	[Export] public InventoryDataNew InventoryData;
	[Export] public Container Slots;

	readonly List<InventorySlotView> _slotViews = new();
	readonly List<InventorySlotViewData> _slotData = new();

	TextureRect _dragPreview;
	InventorySlotViewData _draggedData;
	ItemData _draggedItem;
	int _draggedSourceIndex = -1;
	bool _draggedFromSplit;
	bool _built;

	public override void _Ready()
	{
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		ApplyStyle();
		EnsureSlotDataCount();
		SyncSlotDataFromInventory();
		RefreshSlotViews();
	}

	public override void _Input(InputEvent @event)
	{
		UpdateDragPreviewPosition();

		if (!Visible || !AllowDragAndDrop || _slotViews.Count == 0)
			return;

		if (@event is not InputEventMouseButton mouseEvent || mouseEvent.ButtonIndex != MouseButton.Left)
			return;

		if (mouseEvent.Pressed)
			TryStartDrag(IsSplitModifierPressed(mouseEvent));
		else if (mouseEvent.IsReleased())
			FinishDrag();
	}

	public override void _Process(double delta)
	{
		UpdateDragPreviewPosition();

		if (_draggedData == null && InventoryData != null)
		{
			SyncSlotDataFromInventory();
			RefreshSlotViews();
		}
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Pass;
		ApplyStyle();

		var generatedSlots = new HBoxContainer
		{
			Name = "Slots",
			MouseFilter = MouseFilterEnum.Pass
		};
		generatedSlots.AddThemeConstantOverride("separation", 6);
		Slots = generatedSlots;
		AddChild(Slots);

		for (int i = 0; i < SlotCount; i++)
		{
			var slot = new InventorySlotView
			{
				Name = $"Slot{i + 1}",
				GenerateChildren = true,
				ApplyDefaultStyle = ApplyDefaultStyle
			};
			Slots.AddChild(slot);
			_slotViews.Add(slot);
		}
	}

	public void SetSlots(IReadOnlyList<InventorySlotViewData> slots)
	{
		EnsureSlots();
		EnsureSlotDataCount();

		if (InventoryData != null)
		{
			if (!AcceptEventBusInventory)
			{
				SyncSlotDataFromInventory();
				RefreshSlotViews();
				return;
			}

			EnsureInventoryDataCount();
			for (int i = 0; i < _slotViews.Count; i++)
			{
				InventoryData.itemData[i] = slots != null && i < slots.Count && slots[i] != null
					? slots[i].Item
					: null;
			}

			SyncSlotDataFromInventory();
			RefreshSlotViews();
			return;
		}

		for (int i = 0; i < _slotViews.Count; i++)
		{
			_slotData[i] = slots != null && i < slots.Count && slots[i] != null
				? slots[i].Copy()
				: InventorySlotViewData.Empty();
		}

		RefreshSlotViews();
	}

	public bool TryDropItemAtPosition(Vector2 globalPosition, ItemData item)
	{
		if (item == null)
			return false;

		EnsureSlots();
		EnsureSlotDataCount();

		var targetSlot = FindDropSlot(globalPosition, item);
		var targetIndex = targetSlot == null ? -1 : _slotViews.IndexOf(targetSlot);
		if (targetIndex < 0 || targetIndex >= _slotData.Count)
			return false;

		var targetItem = GetItemAt(targetIndex);
		if (targetItem == null)
		{
			SetSlotAt(targetIndex, item);
			RefreshSlotViews();
			return true;
		}

		if (!IsSameItem(targetItem, item))
			return false;

		targetItem.ItemCount = GetStackCount(targetItem) + GetStackCount(item);
		RefreshSlotViews();
		return true;
	}

	void TryStartDrag(bool splitOne)
	{
		var slot = FindHoveredSlot();
		if (slot == null)
			return;

		var index = _slotViews.IndexOf(slot);
		if (index < 0 || index >= _slotData.Count)
			return;

		var data = _slotData[index];
		if (data == null || data.IsEmpty || data.IsLocked)
			return;

		var sourceItem = GetItemAt(index);
		if (sourceItem == null)
			return;

		var sourceCount = GetStackCount(sourceItem);
		_draggedFromSplit = splitOne && sourceCount > 1;
		_draggedItem = _draggedFromSplit ? MakeStack(sourceItem, 1) : sourceItem;
		_draggedData = InventorySlotViewData.FromItem(_draggedItem);
		_draggedSourceIndex = index;

		if (_draggedFromSplit)
			sourceItem.ItemCount = sourceCount - 1;
		else
			SetSlotAt(index, null);

		CreateDragPreview(_draggedData);
		RefreshSlotViews();
	}

	void FinishDrag()
	{
		if (_draggedData == null)
			return;

		DeleteDragPreview();

		var targetSlot = FindHoveredSlot();
		var targetIndex = targetSlot == null ? -1 : _slotViews.IndexOf(targetSlot);

		if (targetIndex >= 0 && targetIndex < _slotData.Count)
		{
			MoveDraggedDataToSlot(targetIndex);
			return;
		}

		if (_draggedItem != null && TryDropDraggedItemIntoCrafting())
			return;

		
		//Try to drag and drop into the world
		if (_draggedItem != null && TryPlaceItemInWorld(_draggedItem))
		{
			Debug.WriteLine("entered metod");
			ClearDragState();
			return;
		}

		RestoreDraggedData();
	}

	//to be ableto drag and drop
	public bool TryPlaceItemInWorld(ItemData item)
	{

		var world = GetTree().CurrentScene.GetNode("world"); //get access to the world
		if (world == null)
		{
			Debug.WriteLine("No world");
			return false;
		}
		//getting access to the camera through world and player
		var player = world.GetNodeOrNull<CharacterBody2D>("Player");
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
		
		for (int i = 0; i < item.ItemCount; i++)
		{
			var scene = GD.Load<PackedScene>(item.ItemScenePath); //load the scene mentioned in the items scene path
			var itemScene = scene.Instantiate() as Node2D;
			world.AddChild(itemScene);
			itemScene.GlobalPosition = mapPos;
		}
		return true;

	}
	void MoveDraggedDataToSlot(int targetIndex)
	{
		var targetItem = GetItemAt(targetIndex);

		if (targetItem == null)
		{
			SetSlotAt(targetIndex, _draggedItem, _draggedData);
			ClearDragState();
			return;
		}

		if (IsSameItem(targetItem, _draggedItem))
		{
			targetItem.ItemCount = GetStackCount(targetItem) + GetStackCount(_draggedItem);
			ClearDragState();
			return;
		}

		if (_draggedFromSplit)
		{
			RestoreDraggedData();
			return;
		}

		if (targetIndex == _draggedSourceIndex)
		{
			SetSlotAt(targetIndex, _draggedItem, _draggedData);
		}
		else
		{
			var targetData = _slotData[targetIndex];
			SetSlotAt(targetIndex, _draggedItem, _draggedData);

			if (_draggedSourceIndex >= 0 && _draggedSourceIndex < _slotData.Count)
				SetSlotAt(_draggedSourceIndex, targetItem, targetData);
		}

		ClearDragState();
	}

	void RestoreDraggedData()
	{
		if (_draggedSourceIndex >= 0 && _draggedSourceIndex < _slotData.Count)
		{
			var sourceItem = GetItemAt(_draggedSourceIndex);
			if (sourceItem == null)
				SetSlotAt(_draggedSourceIndex, _draggedItem, _draggedData);
			else if (IsSameItem(sourceItem, _draggedItem))
				sourceItem.ItemCount = GetStackCount(sourceItem) + GetStackCount(_draggedItem);
		}

		ClearDragState();
	}

	bool TryDropDraggedItemIntoCrafting()
	{
		if (_draggedItem == null)
			return false;

		var visibleWindow = FindVisibleInventoryWindow(GetTree().Root);
		if (visibleWindow != null && visibleWindow.TryDropItemOnCraftingAtPosition(GetGlobalMousePosition(), _draggedItem))
		{
			ClearDragState();
			GetViewport().SetInputAsHandled();
			return true;
		}

		var hovered = GetViewport().GuiGetHoveredControl();
		var craftingSlot = FindCraftingSlot(hovered);
		if (craftingSlot == null)
			return false;

		var inventoryWindow = FindAncestor<InventoryWindow>(craftingSlot);
		if (inventoryWindow == null)
			return false;

		if (!inventoryWindow.TryDropItemOnCraftingSlot(craftingSlot, _draggedItem))
			return false;

		ClearDragState();
		GetViewport().SetInputAsHandled();
		return true;
	}

	void CreateDragPreview(InventorySlotViewData data)
	{
		DeleteDragPreview();

		_dragPreview = new TextureRect
		{
			Name = "ItemDrag",
			Texture = data.Icon,
			MouseFilter = MouseFilterEnum.Ignore,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Size = DragPreviewSize,
			CustomMinimumSize = DragPreviewSize,
			ZIndex = 100
		};
		AddChild(_dragPreview);
		UpdateDragPreviewPosition();
	}

	void DeleteDragPreview()
	{
		if (_dragPreview == null)
			return;

		_dragPreview.QueueFree();
		_dragPreview = null;
	}

	void UpdateDragPreviewPosition()
	{
		if (_dragPreview == null)
			return;

		_dragPreview.GlobalPosition = GetGlobalMousePosition() - _dragPreview.Size * 0.5f;
	}

	InventorySlotView FindHoveredSlot()
	{
		var hovered = GetViewport().GuiGetHoveredControl();

		while (hovered != null)
		{
			if (hovered is InventorySlotView slot && _slotViews.Contains(slot))
				return slot;

			hovered = hovered.GetParent() as Control;
		}

		return null;
	}

	InventorySlotView FindSlotAtPosition(Vector2 globalPosition)
	{
		foreach (var slot in _slotViews)
		{
			if (slot.GetGlobalRect().HasPoint(globalPosition))
				return slot;
		}

		return null;
	}

	InventorySlotView FindDropSlot(Vector2 globalPosition, ItemData item)
	{
		var exactSlot = FindSlotAtPosition(globalPosition);
		if (exactSlot != null)
			return exactSlot;

		if (!ContainsPointWithMargin(this, globalPosition, 18f) && !ContainsPointWithMargin(Slots, globalPosition, 18f))
			return null;

		var stackSlot = FindFirstSlotMatching(item);
		if (stackSlot != null)
			return stackSlot;

		var emptySlot = FindFirstEmptySlot();
		if (emptySlot != null)
			return emptySlot;

		return FindNearestSlot(globalPosition);
	}

	InventorySlotView FindFirstSlotMatching(ItemData item)
	{
		if (item == null)
			return null;

		for (int i = 0; i < _slotViews.Count; i++)
		{
			if (IsSameItem(GetItemAt(i), item))
				return _slotViews[i];
		}

		return null;
	}

	InventorySlotView FindFirstEmptySlot()
	{
		for (int i = 0; i < _slotViews.Count; i++)
		{
			if (GetItemAt(i) == null)
				return _slotViews[i];
		}

		return null;
	}

	InventorySlotView FindNearestSlot(Vector2 globalPosition)
	{
		InventorySlotView nearest = null;
		var nearestDistance = float.MaxValue;

		foreach (var slot in _slotViews)
		{
			var rect = slot.GetGlobalRect();
			var center = rect.Position + rect.Size * 0.5f;
			var distance = center.DistanceSquaredTo(globalPosition);
			if (distance >= nearestDistance)
				continue;

			nearest = slot;
			nearestDistance = distance;
		}

		return nearest;
	}

	void RefreshSlotViews()
	{
		EnsureSlotDataCount();
		SyncSlotDataFromInventory();

		for (int i = 0; i < _slotViews.Count; i++)
			_slotViews[i].SetData(_slotData[i]);
	}

	void EnsureSlots()
	{
		BindSceneNodes();
		if (_slotViews.Count == 0 && GenerateChildren)
			Build();
	}

	void EnsureSlotDataCount()
	{
		while (_slotData.Count < _slotViews.Count)
			_slotData.Add(InventorySlotViewData.Empty());

		while (_slotData.Count > _slotViews.Count)
			_slotData.RemoveAt(_slotData.Count - 1);
	}

	void EnsureInventoryDataCount()
	{
		if (InventoryData == null)
			return;

		InventoryData.itemData ??= new Godot.Collections.Array<ItemData>();
		while (InventoryData.itemData.Count < _slotViews.Count)
			InventoryData.itemData.Add(null);
	}

	void SyncSlotDataFromInventory()
	{
		if (InventoryData == null || _slotViews.Count == 0)
			return;

		EnsureInventoryDataCount();
		for (int i = 0; i < _slotViews.Count; i++)
			_slotData[i] = InventorySlotViewData.FromItem(InventoryData.itemData[i]);
	}

	ItemData GetItemAt(int index)
	{
		if (index < 0 || index >= _slotData.Count)
			return null;

		if (InventoryData != null)
		{
			EnsureInventoryDataCount();
			return InventoryData.itemData[index];
		}

		return _slotData[index]?.Item;
	}

	void SetSlotAt(int index, ItemData item, InventorySlotViewData fallbackData = null)
	{
		if (index < 0 || index >= _slotData.Count)
			return;

		if (item != null && item.ItemCount <= 0)
			item.ItemCount = 1;

		if (InventoryData != null)
		{
			EnsureInventoryDataCount();
			InventoryData.itemData[index] = item;
			_slotData[index] = InventorySlotViewData.FromItem(item);
			return;
		}

		_slotData[index] = item != null
			? InventorySlotViewData.FromItem(item)
			: fallbackData?.Copy() ?? InventorySlotViewData.Empty();
	}

	void ClearDragState()
	{
		_draggedData = null;
		_draggedItem = null;
		_draggedSourceIndex = -1;
		_draggedFromSplit = false;
		RefreshSlotViews();
	}

	void BindSceneNodes()
	{
		Slots ??= FindChild("Slots", true, false) as Container;
		_slotViews.Clear();

		Node root = Slots != null ? Slots : this;
		foreach (Node child in root.GetChildren())
		{
			if (child is InventorySlotView slot)
			{
				slot.MouseFilter = MouseFilterEnum.Pass;
				_slotViews.Add(slot);
			}
		}

		MouseFilter = MouseFilterEnum.Pass;
		if (Slots != null)
			Slots.MouseFilter = MouseFilterEnum.Pass;
	}

	void ApplyStyle()
	{
		if (ApplyDefaultStyle)
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(new Color(0f, 0f, 0f, 0.18f), 0, 0));
	}

	Slot FindCraftingSlot(Control hovered)
	{
		while (hovered != null)
		{
			if (hovered is Slot slot)
				return slot;

			hovered = hovered.GetParent() as Control;
		}

		return null;
	}

	T FindAncestor<T>(Node node) where T : Node
	{
		var current = node?.GetParent();
		while (current != null)
		{
			if (current is T typed)
				return typed;

			current = current.GetParent();
		}

		return null;
	}

	InventoryWindow FindVisibleInventoryWindow(Node node)
	{
		if (node == null)
			return null;

		if (node is InventoryWindow window && window.Visible)
			return window;

		foreach (var child in node.GetChildren())
		{
			var found = FindVisibleInventoryWindow(child);
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

	static bool ContainsPointWithMargin(Control control, Vector2 globalPosition, float margin)
	{
		if (control == null)
			return false;

		var rect = control.GetGlobalRect();
		rect.Position -= new Vector2(margin, margin);
		rect.Size += new Vector2(margin * 2f, margin * 2f);
		return rect.HasPoint(globalPosition);
	}
}
