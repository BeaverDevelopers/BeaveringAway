using Godot;

public class PlayerVitalsViewData
{
	public int Health = 20;
	public int MaxHealth = 20;

	public float HealthProgress
	{
		get
		{
			if (MaxHealth <= 0)
				return 0f;
			return Mathf.Clamp(Health / (float)MaxHealth, 0f, 1f);
		}
	}
}
