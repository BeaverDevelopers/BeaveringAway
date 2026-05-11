using System;
using Godot;

[GlobalClass]
public partial class PauseMenu : PanelContainer
{
	public event Action ResumePressed;
	public event Action QuitPressed;

	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle = true;
	[Export] public Label TitleLabel;
	[Export] public Button ResumeButton;
	[Export] public Button SettingsButton;
	[Export] public Button QuitButton;

	bool _built;
	bool _wired;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		ApplyStyle();
		WireButtons();
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Stop;
		ApplyStyle();

		var root = new VBoxContainer
		{
			Name = "PauseMenuContent",
			CustomMinimumSize = new Vector2(280, 170)
		};
		root.AddThemeConstantOverride("separation", 10);
		AddChild(root);

		TitleLabel = UIStyle.MakeLabel("Paused", 20);
		TitleLabel.Name = "TitleLabel";
		TitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(TitleLabel);

		ResumeButton = new Button
		{
			Name = "ResumeButton",
			Text = "Resume"
		};
		UIStyle.ApplyButton(ResumeButton);
		root.AddChild(ResumeButton);

		SettingsButton = new Button
		{
			Name = "SettingsButton",
			Text = "Settings",
			Disabled = true
		};
		UIStyle.ApplyButton(SettingsButton);
		root.AddChild(SettingsButton);

		QuitButton = new Button
		{
			Name = "QuitButton",
			Text = "Quit"
		};
		UIStyle.ApplyButton(QuitButton);
		root.AddChild(QuitButton);

		WireButtons();
	}

	void BindSceneNodes()
	{
		TitleLabel ??= FindChild("TitleLabel", true, false) as Label;
		ResumeButton ??= FindChild("ResumeButton", true, false) as Button;
		SettingsButton ??= FindChild("SettingsButton", true, false) as Button;
		QuitButton ??= FindChild("QuitButton", true, false) as Button;

		if (!ApplyDefaultStyle)
			return;

		if (TitleLabel != null)
			UIStyle.ApplyLabel(TitleLabel, 20);
		if (ResumeButton != null)
			UIStyle.ApplyButton(ResumeButton);
		if (SettingsButton != null)
			UIStyle.ApplyButton(SettingsButton);
		if (QuitButton != null)
			UIStyle.ApplyButton(QuitButton);
	}

	void WireButtons()
	{
		if (_wired)
			return;

		if (ResumeButton == null && QuitButton == null)
			return;

		_wired = true;
		if (ResumeButton != null)
			ResumeButton.Pressed += () => ResumePressed?.Invoke();
		if (QuitButton != null)
			QuitButton.Pressed += () => QuitPressed?.Invoke();
	}

	void ApplyStyle()
	{
		if (ApplyDefaultStyle)
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(UIStyle.Panel));
	}
}
