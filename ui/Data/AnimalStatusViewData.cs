public enum AnimalReturnState
{
	Unknown,
	Hinted,
	CanReturn,
	Returned
}

public class AnimalStatusViewData
{
	public string AnimalId = "";
	public string DisplayName = "";
	public string NeedHint = "";
	public AnimalReturnState ReturnState = AnimalReturnState.Unknown;
}
