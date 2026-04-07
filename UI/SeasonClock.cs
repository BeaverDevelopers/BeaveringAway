using Godot;

public enum Season { Spring, Summer, Autumn, Winter }

public partial class SeasonClock : Control
{
	[Export] public Label SeasonLabel;

	public Season CurrentSeason { get; private set; } = Season.Spring;
	public float Progress { get; private set; }

	static readonly Color[] SeasonColors =
	{
		new Color(0.3f, 0.8f, 0.3f),
		new Color(0.95f, 0.85f, 0.2f),
		new Color(0.9f, 0.5f, 0.1f),
		new Color(0.4f, 0.7f, 0.95f),
	};
	static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

	const float Radius = 38f;
	const float Ring = 8f;

	public override void _Draw()
	{
		var center = new Vector2(45, Radius + 5);

		DrawCircle(center, Radius + 2, new Color(0.12f, 0.12f, 0.15f));

		for (int i = 0; i < 4; i++)
		{
			float start = -Mathf.Pi / 2 + i * Mathf.Pi / 2;
			float end = start + Mathf.Pi / 2;
			var color = SeasonColors[i];
			if (i != (int)CurrentSeason)
				color = color.Darkened(0.55f);
			DrawArc(center, Radius - Ring / 2, start, end, 24, color, Ring, true);
		}

		DrawCircle(center, Radius - Ring - 1, new Color(0.08f, 0.08f, 0.1f));

		for (int i = 0; i < 4; i++)
		{
			float a = -Mathf.Pi / 2 + i * Mathf.Pi / 2;
			var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
			DrawLine(center + dir * (Radius - Ring - 2), center + dir * (Radius + 2),
				new Color(1, 1, 1, 0.35f), 1f, true);
		}

		float total = ((int)CurrentSeason + Progress) / 4f;
		float angle = -Mathf.Pi / 2 + total * Mathf.Tau;
		var handEnd = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (Radius - 2);
		DrawLine(center, handEnd, Colors.White, 2f, true);
		DrawCircle(center, 3f, Colors.White);
	}

	public void SetSeason(Season season, float progress)
	{
		CurrentSeason = season;
		Progress = Mathf.Clamp(progress, 0f, 0.999f);
		if (SeasonLabel != null)
			SeasonLabel.Text = SeasonNames[(int)season];
		QueueRedraw();
	}
}
