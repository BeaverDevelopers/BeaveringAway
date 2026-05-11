using System.Collections.Generic;

public enum RecipeDiscoveryState
{
	Unknown,
	Hinted,
	Discovered
}

public class RecipeIngredientViewData
{
	public string ItemId = "";
	public string DisplayName = "";
	public int Required;
	public int Owned;

	public bool HasEnough => Owned >= Required;
}

public class RecipeViewData
{
	public string RecipeId = "";
	public string DisplayName = "";
	public string Hint = "";
	public float CraftSeconds;
	public RecipeDiscoveryState DiscoveryState = RecipeDiscoveryState.Unknown;
	public readonly List<RecipeIngredientViewData> Ingredients = new();

	public bool IsCraftable
	{
		get
		{
			if (DiscoveryState != RecipeDiscoveryState.Discovered)
				return false;

			foreach (var ingredient in Ingredients)
			{
				if (!ingredient.HasEnough)
					return false;
			}

			return true;
		}
	}
}
