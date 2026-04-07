using Godot;

public partial class PauseMenu : ColorRect
{
	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Color = new Color(0, 0, 0, 0.5f);
		MouseFilter = MouseFilterEnum.Stop;
		Visible = false;

		var panel = new PanelContainer();
		panel.AnchorLeft = 0.5f;
		panel.AnchorRight = 0.5f;
		panel.AnchorTop = 0.5f;
		panel.AnchorBottom = 0.5f;
		panel.OffsetLeft = -130;
		panel.OffsetRight = 130;
		panel.OffsetTop = -110;
		panel.OffsetBottom = 110;
		AddChild(panel);

		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.13f, 0.13f, 0.18f, 0.95f);
		style.CornerRadiusTopLeft = style.CornerRadiusTopRight =
			style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 10;
		style.ContentMarginLeft = style.ContentMarginRight = 24;
		style.ContentMarginTop = style.ContentMarginBottom = 20;
		panel.AddThemeStyleboxOverride("panel", style);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		panel.AddChild(vbox);

		var title = new Label();
		title.Text = "PAUSED";
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 26);
		vbox.AddChild(title);

		vbox.AddChild(new HSeparator());

		var resumeBtn = new Button();
		resumeBtn.Text = "Resume";
		resumeBtn.Pressed += () =>
		{
			GetTree().Paused = false;
			Visible = false;
		};
		vbox.AddChild(resumeBtn);

		var settingsBtn = new Button();
		settingsBtn.Text = "Settings";
		settingsBtn.Disabled = true;
		vbox.AddChild(settingsBtn);

		var quitBtn = new Button();
		quitBtn.Text = "Quit";
		quitBtn.Pressed += () => GetTree().Quit();
		vbox.AddChild(quitBtn);
	}
}
