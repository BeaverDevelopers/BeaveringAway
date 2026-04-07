using Godot;

public partial class GameHUD : CanvasLayer
{
	SeasonClock _clock;
	ProgressBar _hungerBar;
	ProgressBar _tempBar;
	StyleBoxFlat _tempFill;
	Label _hungerValue;
	Label _tempValue;
	PauseMenu _pauseMenu;
	VBoxContainer _rightPanel;

	const int TicksPerSeason = 3600;

	public override void _Ready()
	{
		Layer = 10;
		ProcessMode = ProcessModeEnum.Always;

		var root = new Control();
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		BuildTopLeft(root);
		BuildTopRight(root);

		_pauseMenu = new PauseMenu();
		root.AddChild(_pauseMenu);
	}

	/// <summary>
	/// Call after _Ready to position HUD elements within the map area.
	/// </summary>
	public void SetMapBounds(float mapWidth, float mapHeight)
	{
		_rightPanel.Position = new Vector2(mapWidth - 115, 10);
	}

	void BuildTopLeft(Control parent)
	{
		var btn = new Button();
		btn.Text = "II";
		btn.Position = new Vector2(12, 12);
		btn.CustomMinimumSize = new Vector2(36, 36);
		btn.Pressed += TogglePause;
		parent.AddChild(btn);
	}

	void BuildTopRight(Control parent)
	{
		_rightPanel = new VBoxContainer();
		// Position set by SetMapBounds - no viewport anchors
		_rightPanel.AddThemeConstantOverride("separation", 8);
		_rightPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		_rightPanel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		parent.AddChild(_rightPanel);

		_clock = new SeasonClock();
		_rightPanel.AddChild(_clock);

		// Hunger
		var hBox = new HBoxContainer();
		hBox.AddThemeConstantOverride("separation", 4);
		_rightPanel.AddChild(hBox);

		var hIcon = new Label { Text = "Hunger" };
		hIcon.AddThemeFontSizeOverride("font_size", 11);
		hBox.AddChild(hIcon);

		_hungerBar = MakeBar(new Color(0.85f, 0.45f, 0.1f));
		hBox.AddChild(_hungerBar);

		_hungerValue = new Label { Text = "75" };
		_hungerValue.AddThemeFontSizeOverride("font_size", 11);
		_hungerValue.CustomMinimumSize = new Vector2(24, 0);
		hBox.AddChild(_hungerValue);

		// Temperature
		var tBox = new HBoxContainer();
		tBox.AddThemeConstantOverride("separation", 4);
		_rightPanel.AddChild(tBox);

		var tIcon = new Label { Text = "Temp" };
		tIcon.AddThemeFontSizeOverride("font_size", 11);
		tBox.AddChild(tIcon);

		_tempFill = new StyleBoxFlat();
		_tempFill.BgColor = new Color(0.2f, 0.6f, 0.9f);
		_tempFill.CornerRadiusTopLeft = _tempFill.CornerRadiusTopRight =
			_tempFill.CornerRadiusBottomLeft = _tempFill.CornerRadiusBottomRight = 3;

		_tempBar = MakeBar(new Color(0.2f, 0.6f, 0.9f));
		_tempBar.AddThemeStyleboxOverride("fill", _tempFill);
		tBox.AddChild(_tempBar);

		_tempValue = new Label { Text = "20\u00b0" };
		_tempValue.AddThemeFontSizeOverride("font_size", 11);
		_tempValue.CustomMinimumSize = new Vector2(30, 0);
		tBox.AddChild(_tempValue);
	}

	static ProgressBar MakeBar(Color fillColor)
	{
		var bar = new ProgressBar();
		bar.CustomMinimumSize = new Vector2(50, 14);
		bar.MaxValue = 100;
		bar.ShowPercentage = false;
		bar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		var bg = new StyleBoxFlat();
		bg.BgColor = new Color(0f, 0f, 0f, 0.3f);
		bg.CornerRadiusTopLeft = bg.CornerRadiusTopRight =
			bg.CornerRadiusBottomLeft = bg.CornerRadiusBottomRight = 3;
		bar.AddThemeStyleboxOverride("background", bg);

		var fill = new StyleBoxFlat();
		fill.BgColor = fillColor;
		fill.CornerRadiusTopLeft = fill.CornerRadiusTopRight =
			fill.CornerRadiusBottomLeft = fill.CornerRadiusBottomRight = 3;
		bar.AddThemeStyleboxOverride("fill", fill);
		bar.AddThemeStyleboxOverride("grabber_area", new StyleBoxEmpty());
		bar.AddThemeStyleboxOverride("grabber_area_highlight", new StyleBoxEmpty());

		return bar;
	}

	// --- Input ---

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Escape)
		{
			TogglePause();
			GetViewport().SetInputAsHandled();
		}
	}

	void TogglePause()
	{
		bool pausing = !GetTree().Paused;
		GetTree().Paused = pausing;
		_pauseMenu.Visible = pausing;
	}

	// --- Public update methods ---

	public void UpdateSeason(int tick)
	{
		int total = TicksPerSeason * 4;
		int wrapped = tick % total;
		int idx = wrapped / TicksPerSeason;
		float progress = (wrapped % TicksPerSeason) / (float)TicksPerSeason;
		_clock.SetSeason((Season)idx, progress);
	}

	public void UpdateHunger(float value)
	{
		value = Mathf.Clamp(value, 0, 100);
		_hungerBar.Value = value;
		_hungerValue.Text = ((int)value).ToString();
	}

	public void UpdateTemperature(float value)
	{
		value = Mathf.Clamp(value, -40, 50);
		_tempBar.Value = Mathf.Remap(value, -40, 50, 0, 100);
		_tempValue.Text = $"{(int)value}\u00b0";

		if (value < 0)
			_tempFill.BgColor = new Color(0.3f, 0.5f, 1.0f);
		else if (value < 25)
			_tempFill.BgColor = new Color(0.2f, 0.8f, 0.4f);
		else
			_tempFill.BgColor = new Color(1.0f, 0.3f, 0.2f);
	}
}
