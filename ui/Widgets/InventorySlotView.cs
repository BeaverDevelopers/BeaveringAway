using Godot;

[GlobalClass]
public partial class InventorySlotView : PanelContainer
{
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle;
	[Export] public TextureRect IconRect;
	[Export] public Label CountLabel;
	[Export] public Label NameLabel;
	[Export] public Control EmptySwatch;
	[Export] public bool ShowItemName;
	[Export] public bool ShowSingleItemCount;

	bool _built;

	public override void _Ready()
	{
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		SetData(InventorySlotViewData.Empty());
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		CustomMinimumSize = new Vector2(54, 62);
		MouseFilter = MouseFilterEnum.Pass;

		var root = new VBoxContainer
		{
			Name = "SlotContent",
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddThemeConstantOverride("separation", 1);
		AddChild(root);

		var iconHolder = new Control
		{
			Name = "IconHolder",
			CustomMinimumSize = new Vector2(42, 36),
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddChild(iconHolder);

		EmptySwatch = new ColorRect
		{
			Name = "EmptySwatch",
			AnchorRight = 1,
			AnchorBottom = 1,
			Color = new Color(0f, 0f, 0f, 0.18f),
			MouseFilter = MouseFilterEnum.Ignore
		};
		iconHolder.AddChild(EmptySwatch);

		IconRect = new TextureRect
		{
			Name = "IconRect",
			AnchorRight = 1,
			AnchorBottom = 1,
			ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore
		};
		iconHolder.AddChild(IconRect);

		CountLabel = UIStyle.MakeLabel("", 10);
		CountLabel.Name = "CountLabel";
		CountLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(CountLabel);

		NameLabel = UIStyle.MakeLabel("", 8, true);
		NameLabel.Name = "NameLabel";
		NameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		root.AddChild(NameLabel);
	}

	public void SetData(InventorySlotViewData data)
	{
		EnsureControls();
		data ??= InventorySlotViewData.Empty();

		if (ApplyDefaultStyle)
		{
			var color = data.IsSelected ? UIStyle.Warning : UIStyle.Panel;
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(color, data.IsSelected ? 3 : 2));
		}

		if (IconRect != null)
			IconRect.Texture = data.Icon;
		if (EmptySwatch != null)
			EmptySwatch.Visible = data.Icon == null;
		if (CountLabel != null)
		{
			CountLabel.Text = data.IsEmpty || (!ShowSingleItemCount && data.Count <= 1) ? "" : data.Count.ToString();
		}
		if (NameLabel != null)
			NameLabel.Text = data.IsEmpty || !ShowItemName ? "" : data.DisplayName;

		Modulate = data.IsLocked ? new Color(1f, 1f, 1f, 0.45f) : Colors.White;
	}

	void EnsureControls()
	{
		BindSceneNodes();
		if (IconRect == null && GenerateChildren)
			Build();
	}

	void BindSceneNodes()
	{
		IconRect ??= FindChild("IconRect", true, false) as TextureRect;
		CountLabel ??= FindChild("CountLabel", true, false) as Label;
		NameLabel ??= FindChild("NameLabel", true, false) as Label;
		EmptySwatch ??= FindChild("EmptySwatch", true, false) as Control;

		MouseFilter = MouseFilterEnum.Pass;

		if (IconRect != null)
			IconRect.MouseFilter = MouseFilterEnum.Ignore;
		if (CountLabel != null)
			CountLabel.MouseFilter = MouseFilterEnum.Ignore;
		if (NameLabel != null)
			NameLabel.MouseFilter = MouseFilterEnum.Ignore;
		if (EmptySwatch != null)
			EmptySwatch.MouseFilter = MouseFilterEnum.Ignore;
	}
}
