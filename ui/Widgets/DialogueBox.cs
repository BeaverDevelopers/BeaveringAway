using Godot;

[GlobalClass]
public partial class DialogueBox : PanelContainer
{
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle = true;
	[Export] public Label SpeakerLabel;
	[Export] public Label BodyLabel;
	[Export] public TextureRect PortraitRect;
	[Export] public Control ContinueIndicator;

	bool _built;

	public override void _Ready()
	{
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		ApplyStyle();
		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible)
			return;

		if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("interact"))
		{
			UIEventBus.CloseDialogue();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Stop;
		ApplyStyle();

		var root = new HBoxContainer
		{
			Name = "DialogueContent",
			CustomMinimumSize = new Vector2(720, 110),
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddThemeConstantOverride("separation", 10);
		AddChild(root);

		PortraitRect = new TextureRect
		{
			Name = "PortraitRect",
			CustomMinimumSize = new Vector2(72, 72),
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddChild(PortraitRect);

		var textRoot = new VBoxContainer
		{
			Name = "TextRoot",
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		textRoot.AddThemeConstantOverride("separation", 4);
		root.AddChild(textRoot);

		SpeakerLabel = UIStyle.MakeLabel("", 14);
		SpeakerLabel.Name = "SpeakerLabel";
		SpeakerLabel.AddThemeColorOverride("font_color", UIStyle.Ink);
		textRoot.AddChild(SpeakerLabel);

		BodyLabel = UIStyle.MakeLabel("", 12);
		BodyLabel.Name = "BodyLabel";
		BodyLabel.AddThemeColorOverride("font_color", UIStyle.Ink);
		BodyLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
		textRoot.AddChild(BodyLabel);

		var continueLabel = UIStyle.MakeLabel("Press E", 10, true);
		continueLabel.Name = "ContinueIndicator";
		continueLabel.HorizontalAlignment = HorizontalAlignment.Right;
		continueLabel.AddThemeColorOverride("font_color", UIStyle.PanelLight);
		ContinueIndicator = continueLabel;
		textRoot.AddChild(continueLabel);
	}

	public void SetDialogue(DialogueViewData dialogue)
	{
		EnsureControls();
		dialogue ??= new DialogueViewData();

		Visible = true;
		if (SpeakerLabel != null)
			SpeakerLabel.Text = dialogue.SpeakerName;
		if (BodyLabel != null)
			BodyLabel.Text = dialogue.Body;
		if (PortraitRect != null)
		{
			PortraitRect.Texture = dialogue.Portrait;
			PortraitRect.Visible = dialogue.Portrait != null;
		}
		if (ContinueIndicator != null)
			ContinueIndicator.Visible = dialogue.CanContinue;
	}

	public void Close()
	{
		Visible = false;
	}

	void EnsureControls()
	{
		BindSceneNodes();
		if (BodyLabel == null && GenerateChildren)
			Build();
	}

	void BindSceneNodes()
	{
		SpeakerLabel ??= FindChild("SpeakerLabel", true, false) as Label;
		BodyLabel ??= FindChild("BodyLabel", true, false) as Label;
		PortraitRect ??= FindChild("PortraitRect", true, false) as TextureRect;
		ContinueIndicator ??= FindChild("ContinueIndicator", true, false) as Control;

		if (!ApplyDefaultStyle)
			return;

		if (SpeakerLabel != null)
		{
			UIStyle.ApplyLabel(SpeakerLabel, 14);
			SpeakerLabel.AddThemeColorOverride("font_color", UIStyle.Ink);
		}
		if (BodyLabel != null)
		{
			UIStyle.ApplyLabel(BodyLabel, 12);
			BodyLabel.AddThemeColorOverride("font_color", UIStyle.Ink);
		}
		if (ContinueIndicator is Label continueLabel)
		{
			UIStyle.ApplyLabel(continueLabel, 10, true);
			continueLabel.AddThemeColorOverride("font_color", UIStyle.PanelLight);
		}
	}

	void ApplyStyle()
	{
		if (ApplyDefaultStyle)
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(UIStyle.Paper));
	}
}
