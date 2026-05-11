using Godot;

public enum ObjectiveState
{
	Locked,
	Active,
	Complete
}

public class ObjectiveViewData
{
	public string ObjectiveId = "";
	public string Title = "";
	public string Detail = "";
	public int Current;
	public int Target = 1;
	public ObjectiveState State = ObjectiveState.Active;
	public bool IsTracked;

	public float Progress
	{
		get
		{
			if (Target <= 0)
				return State == ObjectiveState.Complete ? 1f : 0f;
			return Mathf.Clamp(Current / (float)Target, 0f, 1f);
		}
	}

	public bool IsComplete => State == ObjectiveState.Complete || Progress >= 1f;

	public ObjectiveViewData()
	{
	}

	public ObjectiveViewData(string objectiveId, string title, string detail, int current = 0, int target = 1)
	{
		ObjectiveId = objectiveId;
		Title = title;
		Detail = detail;
		Current = current;
		Target = target;
	}
}
