using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class UIRoot : CanvasLayer
{
	[Export] public bool AutoFindSceneNodes = true;
	[Export] public bool GenerateMissingWidgets;
	[Export] public bool ShowDebugPreview;
	[Export] public bool PauseTree = true;

	[ExportGroup("Scene Widgets")]
	[Export] public ObjectiveTracker ObjectiveTracker;
	[Export] public WorldStatusPanel WorldStatusPanel;
	[Export] public HealthBar HealthBar;
	[Export] public InventoryBar InventoryBar;
	[Export] public InteractionPrompt InteractionPrompt;
	[Export] public DialogueBox DialogueBox;
	[Export] public ToastFeed ToastFeed;
	[Export] public PauseMenu PauseMenu;
	[Export] public Control PauseDim;

	Control _generatedRoot;
	bool _generatedLayoutBuilt;
	bool _pauseMenuWired;
	bool _paused;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		if (Layer == 1)
			Layer = 20;

		if (AutoFindSceneNodes)
			BindSceneNodes();

		if (GenerateMissingWidgets)
			BuildGeneratedLayout();

		WirePauseMenu();
		Subscribe();

		if (ShowDebugPreview)
			ShowPreviewData();
	}

	public override void _ExitTree()
	{
		Unsubscribe();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
		{
			TogglePause();
			GetViewport().SetInputAsHandled();
		}
	}

	public void BindSceneNodes()
	{
		ObjectiveTracker ??= FindWidget<ObjectiveTracker>("ObjectiveTracker");
		WorldStatusPanel ??= FindWidget<WorldStatusPanel>("WorldStatusPanel");
		HealthBar ??= FindWidget<HealthBar>("HealthBar");
		InventoryBar ??= FindWidget<InventoryBar>("InventoryBar");
		InteractionPrompt ??= FindWidget<InteractionPrompt>("InteractionPrompt");
		DialogueBox ??= FindWidget<DialogueBox>("DialogueBox");
		ToastFeed ??= FindWidget<ToastFeed>("ToastFeed");
		PauseMenu ??= FindWidget<PauseMenu>("PauseMenu");
		PauseDim ??= FindWidget<Control>("PauseDim");
	}

	public void SetWorldStatus(WorldStatusViewData status)
	{
		EnsureWidgetsIfRequested();
		WorldStatusPanel?.SetStatus(status);
	}

	public void SetObjectives(IReadOnlyList<ObjectiveViewData> objectives)
	{
		EnsureWidgetsIfRequested();
		ObjectiveTracker?.SetObjectives(objectives);
	}

	public void SetInventory(IReadOnlyList<InventorySlotViewData> slots)
	{
		EnsureWidgetsIfRequested();
		InventoryBar?.SetSlots(slots);
	}

	public void SetPlayerVitals(PlayerVitalsViewData vitals)
	{
		EnsureWidgetsIfRequested();
		HealthBar?.SetVitals(vitals);
	}

	public void SetInteractionPrompt(string text, bool visible)
	{
		EnsureWidgetsIfRequested();
		InteractionPrompt?.SetPrompt(text, visible);
	}

	public void SetInteractionPromptForTarget(string text, bool visible, Node2D target, Vector2 screenOffset)
	{
		EnsureWidgetsIfRequested();
		InteractionPrompt?.SetPromptForTarget(text, visible, target, screenOffset);
	}

	public void ShowDialogue(DialogueViewData dialogue)
	{
		EnsureWidgetsIfRequested();
		DialogueBox?.SetDialogue(dialogue);
	}

	public void CloseDialogue()
	{
		EnsureWidgetsIfRequested();
		DialogueBox?.Close();
	}

	public void ShowToast(string message, ToastKind kind = ToastKind.Info, float seconds = 2.5f)
	{
		EnsureWidgetsIfRequested();
		ToastFeed?.ShowToast(message, kind, seconds);
	}

	public void SetPauseMenuVisible(bool visible)
	{
		EnsureWidgetsIfRequested();
		SetPaused(visible);
	}

	public void TogglePause()
	{
		SetPaused(!_paused);
	}

	void EnsureWidgetsIfRequested()
	{
		if (GenerateMissingWidgets && !_generatedLayoutBuilt)
			BuildGeneratedLayout();
	}

	void BuildGeneratedLayout()
	{
		if (_generatedLayoutBuilt)
			return;

		_generatedLayoutBuilt = true;

		_generatedRoot = MakeFullRectControl("GeneratedUIRoot");
		AddChild(_generatedRoot);

		var hudLayer = MakeFullRectControl("HudLayer");
		_generatedRoot.AddChild(hudLayer);

		var modalLayer = MakeFullRectControl("ModalLayer");
		_generatedRoot.AddChild(modalLayer);

		ObjectiveTracker ??= new ObjectiveTracker
		{
			Name = "ObjectiveTracker",
			GenerateChildren = true,
			AnchorLeft = 0,
			AnchorTop = 0,
			AnchorRight = 0,
			AnchorBottom = 0,
			OffsetLeft = 16,
			OffsetTop = 16,
			OffsetRight = 316,
			OffsetBottom = 180
		};
		if (ObjectiveTracker.GetParent() == null)
			hudLayer.AddChild(ObjectiveTracker);

		WorldStatusPanel ??= new WorldStatusPanel
		{
			Name = "WorldStatusPanel",
			GenerateChildren = true,
			AnchorLeft = 1,
			AnchorTop = 0,
			AnchorRight = 1,
			AnchorBottom = 0,
			OffsetLeft = -292,
			OffsetTop = 16,
			OffsetRight = -16,
			OffsetBottom = 170
		};
		if (WorldStatusPanel.GetParent() == null)
			hudLayer.AddChild(WorldStatusPanel);

		HealthBar ??= new HealthBar
		{
			Name = "HealthBar",
			GenerateChildren = true,
			AnchorLeft = 0.5f,
			AnchorTop = 1,
			AnchorRight = 0.5f,
			AnchorBottom = 1,
			OffsetLeft = -120,
			OffsetTop = -112,
			OffsetRight = 120,
			OffsetBottom = -88
		};
		if (HealthBar.GetParent() == null)
			hudLayer.AddChild(HealthBar);

		InventoryBar ??= new InventoryBar
		{
			Name = "InventoryBar",
			GenerateChildren = true,
			AnchorLeft = 0.5f,
			AnchorTop = 1,
			AnchorRight = 0.5f,
			AnchorBottom = 1,
			OffsetLeft = -250,
			OffsetTop = -86,
			OffsetRight = 250,
			OffsetBottom = -16
		};
		if (InventoryBar.GetParent() == null)
			hudLayer.AddChild(InventoryBar);

		InteractionPrompt ??= new InteractionPrompt
		{
			Name = "InteractionPrompt",
			GenerateChildren = true,
			AnchorLeft = 0.5f,
			AnchorTop = 1,
			AnchorRight = 0.5f,
			AnchorBottom = 1,
			OffsetLeft = -120,
			OffsetTop = -150,
			OffsetRight = 120,
			OffsetBottom = -110
		};
		if (InteractionPrompt.GetParent() == null)
			hudLayer.AddChild(InteractionPrompt);

		ToastFeed ??= new ToastFeed
		{
			Name = "ToastFeed",
			AnchorLeft = 0.5f,
			AnchorTop = 0,
			AnchorRight = 0.5f,
			AnchorBottom = 0,
			OffsetLeft = -150,
			OffsetTop = 18,
			OffsetRight = 150,
			OffsetBottom = 300
		};
		if (ToastFeed.GetParent() == null)
			hudLayer.AddChild(ToastFeed);

		PauseDim ??= new ColorRect
		{
			Name = "PauseDim",
			AnchorRight = 1,
			AnchorBottom = 1,
			Color = new Color(0f, 0f, 0f, 0.52f),
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Stop,
			ProcessMode = ProcessModeEnum.Always
		};
		if (PauseDim.GetParent() == null)
			modalLayer.AddChild(PauseDim);

		DialogueBox ??= new DialogueBox
		{
			Name = "DialogueBox",
			GenerateChildren = true,
			AnchorLeft = 0.5f,
			AnchorTop = 1,
			AnchorRight = 0.5f,
			AnchorBottom = 1,
			OffsetLeft = -380,
			OffsetTop = -150,
			OffsetRight = 380,
			OffsetBottom = -28,
			ProcessMode = ProcessModeEnum.Always
		};
		if (DialogueBox.GetParent() == null)
			modalLayer.AddChild(DialogueBox);

		PauseMenu ??= new PauseMenu
		{
			Name = "PauseMenu",
			GenerateChildren = true,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = -150,
			OffsetTop = -100,
			OffsetRight = 150,
			OffsetBottom = 100,
			Visible = false,
			ProcessMode = ProcessModeEnum.Always
		};
		if (PauseMenu.GetParent() == null)
			modalLayer.AddChild(PauseMenu);

		WirePauseMenu();
	}

	void SetPaused(bool paused)
	{
		_paused = paused;

		if (PauseTree)
			GetTree().Paused = paused;

		if (PauseDim != null)
			PauseDim.Visible = paused;

		if (PauseMenu != null)
			PauseMenu.Visible = paused;
	}

	void Subscribe()
	{
		UIEventBus.WorldStatusChanged += SetWorldStatus;
		UIEventBus.ObjectivesChanged += SetObjectives;
		UIEventBus.InventoryChanged += SetInventory;
		UIEventBus.PlayerVitalsChanged += SetPlayerVitals;
		UIEventBus.InteractionPromptChanged += SetInteractionPrompt;
		UIEventBus.InteractionPromptTargetChanged += SetInteractionPromptForTarget;
		UIEventBus.DialogueShown += ShowDialogue;
		UIEventBus.DialogueClosed += CloseDialogue;
		UIEventBus.ToastRequested += ShowToast;
		UIEventBus.PauseMenuVisibilityChanged += SetPauseMenuVisible;
		UIEventBus.PauseToggleRequested += TogglePause;
		UIEventBus.QuitRequested += QuitGame;
	}

	void Unsubscribe()
	{
		UIEventBus.WorldStatusChanged -= SetWorldStatus;
		UIEventBus.ObjectivesChanged -= SetObjectives;
		UIEventBus.InventoryChanged -= SetInventory;
		UIEventBus.PlayerVitalsChanged -= SetPlayerVitals;
		UIEventBus.InteractionPromptChanged -= SetInteractionPrompt;
		UIEventBus.InteractionPromptTargetChanged -= SetInteractionPromptForTarget;
		UIEventBus.DialogueShown -= ShowDialogue;
		UIEventBus.DialogueClosed -= CloseDialogue;
		UIEventBus.ToastRequested -= ShowToast;
		UIEventBus.PauseMenuVisibilityChanged -= SetPauseMenuVisible;
		UIEventBus.PauseToggleRequested -= TogglePause;
		UIEventBus.QuitRequested -= QuitGame;
	}

	void WirePauseMenu()
	{
		if (_pauseMenuWired || PauseMenu == null)
			return;

		_pauseMenuWired = true;
		PauseMenu.ResumePressed += () => SetPaused(false);
		PauseMenu.QuitPressed += () => UIEventBus.QuitGame();
	}

	void QuitGame()
	{
		GetTree().Quit();
	}

	void ShowPreviewData()
	{
		SetWorldStatus(new WorldStatusViewData
		{
			WaterTiles = 128,
			RestoredTiles = 12,
			TotalRecoverableTiles = 80,
			AnimalFriends = 1,
			TotalAnimalFriends = 8,
			CurrentFocus = "Redirect water to dry grass"
		});

		SetObjectives(new List<ObjectiveViewData>
		{
			new("restore_grass", "Restore dry grass", "Bring water back to the forest floor", 12, 80) { IsTracked = true },
			new("invite_rabbit", "Bring back Rabbit", "Grow enough good grass", 0, 1)
		});

		SetPlayerVitals(new PlayerVitalsViewData
		{
			Health = 16,
			MaxHealth = 20
		});

		var logItem = GD.Load<ItemData>("res://Resources/logs.tres")?.Duplicate() as ItemData;
		if (logItem != null)
			logItem.ItemCount = 3;
		SetInventory(new List<InventorySlotViewData>
		{
			InventorySlotViewData.FromItem(logItem, true, "Log")
		});

		ShowToast("New goal added", ToastKind.Info, 3f);
	}

	T FindWidget<T>(string nodeName) where T : Node
	{
		return FindChild(nodeName, true, false) as T;
	}

	static Control MakeFullRectControl(string name)
	{
		return new Control
		{
			Name = name,
			AnchorRight = 1,
			AnchorBottom = 1,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}
}
