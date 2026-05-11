using Godot;

public class DialogueViewData
{
	public string SpeakerName = "";
	public string Body = "";
	public Texture2D Portrait;
	public bool CanContinue = true;

	public DialogueViewData()
	{
	}

	public DialogueViewData(string speakerName, string body, Texture2D portrait = null)
	{
		SpeakerName = speakerName;
		Body = body;
		Portrait = portrait;
	}
}
