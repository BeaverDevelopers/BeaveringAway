using Godot;

public class WorldStatusViewData
{
	public int WaterTiles;
	public int RestoredTiles;
	public int TotalRecoverableTiles = 1;
	public int AnimalFriends;
	public int TotalAnimalFriends = 1;
	public string CurrentFocus = "";
	public bool IsFoxThreatActive;
	public bool IsDangerousWaterNearby;

	public float RestoredProgress
	{
		get
		{
			if (TotalRecoverableTiles <= 0)
				return 0f;
			return Mathf.Clamp(RestoredTiles / (float)TotalRecoverableTiles, 0f, 1f);
		}
	}

	public float AnimalProgress
	{
		get
		{
			if (TotalAnimalFriends <= 0)
				return 0f;
			return Mathf.Clamp(AnimalFriends / (float)TotalAnimalFriends, 0f, 1f);
		}
	}
}
