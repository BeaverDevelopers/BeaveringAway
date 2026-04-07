using Godot;

public partial class GameHUD : CanvasLayer
{
	[Export] public SeasonClock Clock;
	[Export] public ProgressBar HungerBar;
	[Export] public ProgressBar TempBar;
	[Export] public Label HungerValue;
	[Export] public Label TempValue;
	[Export] public Button PauseButton;
	[Export] public Control PauseOverlay;
	[Export] public Button ResumeBtn;
	[Export] public Button QuitBtn;
	[Export] public VBoxContainer RightPanel;

	StyleBoxFlat _tempFill;
	const int TicksPerSeason = 3600;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		PauseButton.Pressed += TogglePause;
		ResumeBtn.Pressed += TogglePause;
		QuitBtn.Pressed += () => GetTree().Quit();

		_tempFill = (StyleBoxFlat)TempBar.GetThemeStylebox("fill").Duplicate();
		TempBar.AddThemeStyleboxOverride("fill", _tempFill);
	}

	public void SetMapBounds(float mapWidth, float mapHeight)
	{
		RightPanel.Position = new Vector2(mapWidth - 115, 10);
	}

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
		PauseOverlay.Visible = pausing;
	}

	public void UpdateSeason(int tick)
	{
		int total = TicksPerSeason * 4;
		int wrapped = tick % total;
		int idx = wrapped / TicksPerSeason;
		float progress = (wrapped % TicksPerSeason) / (float)TicksPerSeason;
		Clock.SetSeason((Season)idx, progress);
	}

	public void UpdateHunger(float value)
	{
		value = Mathf.Clamp(value, 0, 100);
		HungerBar.Value = value;
		HungerValue.Text = ((int)value).ToString();
	}

	public void UpdateTemperature(float value)
	{
		value = Mathf.Clamp(value, -40, 50);
		TempBar.Value = Mathf.Remap(value, -40, 50, 0, 100);
		TempValue.Text = $"{(int)value}\u00b0";

		if (value < 0)
			_tempFill.BgColor = new Color(0.3f, 0.5f, 1.0f);
		else if (value < 25)
			_tempFill.BgColor = new Color(0.2f, 0.8f, 0.4f);
		else
			_tempFill.BgColor = new Color(1.0f, 0.3f, 0.2f);
	}
}
