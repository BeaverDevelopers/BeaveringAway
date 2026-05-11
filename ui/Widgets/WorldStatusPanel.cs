using Godot;

[GlobalClass]
public partial class WorldStatusPanel : PanelContainer
{
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle = true;
	[Export] public Label HeaderLabel;
	[Export] public Label RestoredLabel;
	[Export] public ProgressBar RestoredBar;
	[Export] public Label AnimalsLabel;
	[Export] public ProgressBar AnimalBar;

	bool _built;

	public override void _Ready()
	{
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		ApplyStyle();
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Ignore;
		ApplyStyle();

		var root = new VBoxContainer
		{
			Name = "WorldStatusList",
			CustomMinimumSize = new Vector2(250, 0),
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddThemeConstantOverride("separation", 6);
		AddChild(root);

		HeaderLabel = UIStyle.MakeLabel("World", 13);
		HeaderLabel.Name = "HeaderLabel";
		root.AddChild(HeaderLabel);

		RestoredLabel = UIStyle.MakeLabel("Land", 10);
		RestoredLabel.Name = "RestoredLabel";
		root.AddChild(RestoredLabel);

		RestoredBar = new ProgressBar
		{
			Name = "RestoredBar",
			CustomMinimumSize = new Vector2(220, 9),
			MinValue = 0,
			MaxValue = 100
		};
		UIStyle.ApplyProgressBar(RestoredBar, UIStyle.RestoreGreen);
		root.AddChild(RestoredBar);

		AnimalsLabel = UIStyle.MakeLabel("Friends", 10);
		AnimalsLabel.Name = "AnimalsLabel";
		root.AddChild(AnimalsLabel);

		AnimalBar = new ProgressBar
		{
			Name = "AnimalBar",
			CustomMinimumSize = new Vector2(220, 9),
			MinValue = 0,
			MaxValue = 100
		};
		UIStyle.ApplyProgressBar(AnimalBar, UIStyle.Warning);
		root.AddChild(AnimalBar);

	}

	public void SetStatus(WorldStatusViewData status)
	{
		EnsureControls();
		status ??= new WorldStatusViewData();

		if (RestoredLabel != null)
			RestoredLabel.Text = $"Land {Mathf.RoundToInt(status.RestoredProgress * 100f)}%";
		if (RestoredBar != null)
			RestoredBar.Value = status.RestoredProgress * 100.0;
		if (AnimalsLabel != null)
			AnimalsLabel.Text = $"Friends {status.AnimalFriends}/{status.TotalAnimalFriends}";
		if (AnimalBar != null)
			AnimalBar.Value = status.AnimalProgress * 100.0;
	}

	void EnsureControls()
	{
		BindSceneNodes();
		if (RestoredBar == null && GenerateChildren)
			Build();
	}

	void BindSceneNodes()
	{
		HeaderLabel ??= FindChild("HeaderLabel", true, false) as Label;
		RestoredLabel ??= FindChild("RestoredLabel", true, false) as Label;
		RestoredBar ??= FindChild("RestoredBar", true, false) as ProgressBar;
		AnimalsLabel ??= FindChild("AnimalsLabel", true, false) as Label;
		AnimalBar ??= FindChild("AnimalBar", true, false) as ProgressBar;

		if (!ApplyDefaultStyle)
			return;

		if (HeaderLabel != null)
			UIStyle.ApplyLabel(HeaderLabel, 13);
		if (RestoredLabel != null)
			UIStyle.ApplyLabel(RestoredLabel, 10);
		if (AnimalsLabel != null)
			UIStyle.ApplyLabel(AnimalsLabel, 10);
		if (RestoredBar != null)
			UIStyle.ApplyProgressBar(RestoredBar, UIStyle.RestoreGreen);
		if (AnimalBar != null)
			UIStyle.ApplyProgressBar(AnimalBar, UIStyle.Warning);
	}

	void ApplyStyle()
	{
		if (ApplyDefaultStyle)
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(UIStyle.Panel));
	}
}
