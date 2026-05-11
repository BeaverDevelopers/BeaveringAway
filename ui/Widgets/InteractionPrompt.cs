using Godot;

[GlobalClass]
public partial class InteractionPrompt : PanelContainer
{
	[Export] public string ActionKey = "E";
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle;
	[Export] public bool FollowTargetInWorld;
	[Export] public Vector2 ScreenOffset = new(0, -72);
	[Export] public HBoxContainer PromptContent;
	[Export] public Label KeyLabel;
	[Export] public Label PromptLabel;

	Node2D _target;
	bool _built;

	public override void _Ready()
	{
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		ApplyStyle();
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!Visible || !FollowTargetInWorld || _target == null || !GodotObject.IsInstanceValid(_target))
			return;

		var canvasTransform = GetViewport().GetCanvasTransform();
		var screenPosition = canvasTransform * _target.GlobalPosition + ScreenOffset;
		Position = screenPosition - Size * 0.5f;
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Ignore;
		ApplyStyle();

		var root = new HBoxContainer
		{
			Name = "PromptContent",
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddThemeConstantOverride("separation", 8);
		AddChild(root);
		PromptContent = root;

		KeyLabel = UIStyle.MakeLabel(ActionKey, 14);
		KeyLabel.Name = "KeyLabel";
		KeyLabel.HorizontalAlignment = HorizontalAlignment.Center;
		KeyLabel.CustomMinimumSize = new Vector2(24, 24);
		root.AddChild(KeyLabel);

		PromptLabel = UIStyle.MakeLabel("", 12);
		PromptLabel.Name = "PromptLabel";
		root.AddChild(PromptLabel);
	}

	public void SetPrompt(string text, bool visible)
	{
		EnsureControls();
		FollowTargetInWorld = false;
		_target = null;
		Visible = visible && !string.IsNullOrEmpty(text);

		if (PromptLabel != null)
			PromptLabel.Text = text ?? "";
		if (KeyLabel != null)
			KeyLabel.Text = ActionKey;
	}

	public void SetPromptForTarget(string text, bool visible, Node2D target, Vector2 screenOffset)
	{
		EnsureControls();
		_target = target;
		ScreenOffset = screenOffset;
		FollowTargetInWorld = target != null;
		Visible = visible && !string.IsNullOrEmpty(text) && target != null;

		if (PromptLabel != null)
			PromptLabel.Text = text ?? "";
		if (KeyLabel != null)
			KeyLabel.Text = ActionKey;
	}

	void EnsureControls()
	{
		BindSceneNodes();
		if (PromptLabel == null && GenerateChildren)
			Build();
	}

	void BindSceneNodes()
	{
		PromptContent ??= FindChild("PromptContent", true, false) as HBoxContainer;
		KeyLabel ??= FindChild("KeyLabel", true, false) as Label;
		PromptLabel ??= FindChild("PromptLabel", true, false) as Label;

		if (!ApplyDefaultStyle)
			return;

		if (KeyLabel != null)
			UIStyle.ApplyLabel(KeyLabel, 14);
		if (PromptLabel != null)
			UIStyle.ApplyLabel(PromptLabel, 12);
	}

	void ApplyStyle()
	{
		if (ApplyDefaultStyle)
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(UIStyle.Panel));
	}
}
