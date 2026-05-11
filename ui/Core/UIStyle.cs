using Godot;

public static class UIStyle
{
	public const string PixelFontPath = "res://ui/Resources/Fonts/pixelFont-7-8x14-sproutLands.ttf";

	public static readonly Color Ink = new(0.12f, 0.09f, 0.07f, 1f);
	public static readonly Color Paper = new(0.95f, 0.88f, 0.69f, 0.96f);
	public static readonly Color Panel = new(0.23f, 0.17f, 0.11f, 0.92f);
	public static readonly Color PanelLight = new(0.45f, 0.31f, 0.18f, 0.96f);
	public static readonly Color Border = new(0.09f, 0.06f, 0.04f, 1f);
	public static readonly Color Text = new(0.98f, 0.93f, 0.80f, 1f);
	public static readonly Color MutedText = new(0.77f, 0.69f, 0.56f, 1f);
	public static readonly Color RestoreGreen = new(0.36f, 0.72f, 0.35f, 1f);
	public static readonly Color WaterBlue = new(0.28f, 0.58f, 0.86f, 1f);
	public static readonly Color Warning = new(0.95f, 0.64f, 0.24f, 1f);
	public static readonly Color Danger = new(0.86f, 0.22f, 0.18f, 1f);

	public static Font LoadPixelFont()
	{
		return GD.Load<Font>(PixelFontPath);
	}

	public static StyleBoxFlat MakePanelStyle(Color color, int borderWidth = 2, int radius = 4)
	{
		var style = new StyleBoxFlat
		{
			BgColor = color,
			BorderColor = Border
		};
		style.SetBorderWidthAll(borderWidth);
		style.SetCornerRadiusAll(radius);
		return style;
	}

	public static StyleBoxFlat MakeProgressFill(Color color)
	{
		var style = new StyleBoxFlat
		{
			BgColor = color,
			BorderColor = color.Darkened(0.35f)
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(2);
		return style;
	}

	public static void ApplyLabel(Label label, int fontSize = 12, bool muted = false)
	{
		var font = LoadPixelFont();
		if (font != null)
			label.AddThemeFontOverride("font", font);

		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", muted ? MutedText : Text);
	}

	public static Label MakeLabel(string text, int fontSize = 12, bool muted = false)
	{
		var label = new Label
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			VerticalAlignment = VerticalAlignment.Center
		};
		ApplyLabel(label, fontSize, muted);
		return label;
	}

	public static void ApplyButton(Button button)
	{
		var font = LoadPixelFont();
		if (font != null)
			button.AddThemeFontOverride("font", font);

		button.AddThemeFontSizeOverride("font_size", 13);
		button.AddThemeStyleboxOverride("normal", MakePanelStyle(PanelLight));
		button.AddThemeStyleboxOverride("hover", MakePanelStyle(PanelLight.Lightened(0.08f)));
		button.AddThemeStyleboxOverride("pressed", MakePanelStyle(PanelLight.Darkened(0.12f)));
		button.AddThemeStyleboxOverride("disabled", MakePanelStyle(Panel.Darkened(0.12f)));
		button.AddThemeColorOverride("font_color", Text);
		button.AddThemeColorOverride("font_disabled_color", MutedText);
	}

	public static void ApplyProgressBar(ProgressBar bar, Color fillColor)
	{
		bar.ShowPercentage = false;
		bar.AddThemeStyleboxOverride("background", MakePanelStyle(new Color(0f, 0f, 0f, 0.35f), 1, 2));
		bar.AddThemeStyleboxOverride("fill", MakeProgressFill(fillColor));
	}
}
