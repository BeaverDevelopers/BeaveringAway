using Godot;

public partial class StolenItemFloater : Sprite2D
{
	const float Duration = 1.2f;
	const float HeadOffsetY = -56f;
	const float RiseDistance = 28f;

	float _remaining = Duration;

	public void Play(Texture2D icon)
	{
		Texture = icon;
		Centered = true;
		ZIndex = 10;
		Position = new Vector2(0, HeadOffsetY);
		Modulate = Colors.White;
		Scale = Vector2.One * 1.4f;
		_remaining = Duration;
	}

	public override void _Process(double delta)
	{
		_remaining -= (float)delta;
		float alpha = Mathf.Clamp(_remaining / Duration, 0f, 1f);
		Modulate = new Color(1f, 1f, 1f, alpha);
		Position = new Vector2(0, HeadOffsetY - RiseDistance * (1f - alpha));

		if (_remaining <= 0f)
			QueueFree();
	}
}
