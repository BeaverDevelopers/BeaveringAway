using Godot;

[GlobalClass]
public partial class HealthBar : Control
{
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle = true;
	[Export] public int MaxHearts = 10;
	[Export] public HBoxContainer Hearts;
	[Export] public Texture2D FullHeartIcon;
	[Export] public Texture2D EmptyHeartIcon;
	[Export] public string FullHeartText = "♥";
	[Export] public string EmptyHeartText = "♡";

	bool _built;
	int _health = 20;
	int _maxHealth = 20;

	public override void _Ready()
	{
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		SetHealth(_health, _maxHealth);
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Ignore;

		Hearts = new HBoxContainer
		{
			Name = "Hearts",
			MouseFilter = MouseFilterEnum.Ignore
		};
		Hearts.AddThemeConstantOverride("separation", 2);
		AddChild(Hearts);

		for (int i = 0; i < MaxHearts; i++)
		{
			var heart = new Label
			{
				Name = $"Heart{i + 1}",
				Text = FullHeartText,
				HorizontalAlignment = HorizontalAlignment.Center,
				CustomMinimumSize = new Vector2(18, 18),
				MouseFilter = MouseFilterEnum.Ignore
			};
			if (ApplyDefaultStyle)
			{
				UIStyle.ApplyLabel(heart, 16);
				heart.AddThemeColorOverride("font_color", UIStyle.Danger);
			}
			Hearts.AddChild(heart);
		}
	}

	public void SetVitals(PlayerVitalsViewData vitals)
	{
		vitals ??= new PlayerVitalsViewData();
		SetHealth(vitals.Health, vitals.MaxHealth);
	}

	public void SetHealth(int health, int maxHealth)
	{
		_health = Mathf.Max(0, health);
		_maxHealth = Mathf.Max(1, maxHealth);

		EnsureHearts();
		if (Hearts == null)
			return;

		int requiredHearts = Mathf.Clamp(Mathf.CeilToInt(_maxHealth / 2f), 1, MaxHearts);
		int fullHearts = Mathf.Clamp(Mathf.CeilToInt(_health / 2f), 0, requiredHearts);

		for (int i = 0; i < Hearts.GetChildCount(); i++)
		{
			var child = Hearts.GetChild(i);
			bool isUsed = i < requiredHearts;
			bool isFull = i < fullHearts;

			if (child is Control control)
				control.Visible = isUsed;

			if (child is TextureRect textureRect)
			{
				textureRect.Texture = isFull ? FullHeartIcon : EmptyHeartIcon;
				textureRect.Modulate = isFull ? Colors.White : new Color(1f, 1f, 1f, 0.35f);
			}
			else if (child is Label label)
			{
				label.Text = isFull ? FullHeartText : EmptyHeartText;
				if (ApplyDefaultStyle)
					label.AddThemeColorOverride("font_color", isFull ? UIStyle.Danger : UIStyle.MutedText);
			}
		}
	}

	void EnsureHearts()
	{
		BindSceneNodes();
		if ((Hearts == null || Hearts.GetChildCount() == 0) && GenerateChildren)
			Build();
	}

	void BindSceneNodes()
	{
		Hearts ??= FindChild("Hearts", true, false) as HBoxContainer;
	}
}
