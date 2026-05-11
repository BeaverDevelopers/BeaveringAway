using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class UIEventBus : Node
{
	public static event Action<WorldStatusViewData> WorldStatusChanged;
	public static event Action<IReadOnlyList<ObjectiveViewData>> ObjectivesChanged;
	public static event Action<string> ObjectiveTrackingRequested;
	public static event Action<IReadOnlyList<InventorySlotViewData>> InventoryChanged;
	public static event Action<PlayerVitalsViewData> PlayerVitalsChanged;
	public static event Action<string, bool> InteractionPromptChanged;
	public static event Action<string, bool, Node2D, Vector2> InteractionPromptTargetChanged;
	public static event Action<DialogueViewData> DialogueShown;
	public static event Action DialogueClosed;
	public static event Action<string, ToastKind, float> ToastRequested;
	public static event Action<bool> PauseMenuVisibilityChanged;
	public static event Action PauseToggleRequested;
	public static event Action QuitRequested;

	public static void SetWorldStatus(WorldStatusViewData status)
	{
		WorldStatusChanged?.Invoke(status ?? new WorldStatusViewData());
	}

	public static void SetObjectives(IEnumerable<ObjectiveViewData> objectives)
	{
		ObjectivesChanged?.Invoke(objectives == null
			? new List<ObjectiveViewData>()
			: new List<ObjectiveViewData>(objectives));
	}

	public static void RequestObjectiveTracking(string objectiveId)
	{
		ObjectiveTrackingRequested?.Invoke(objectiveId ?? "");
	}

	public static void SetInventory(IEnumerable<InventorySlotViewData> slots)
	{
		InventoryChanged?.Invoke(slots == null
			? new List<InventorySlotViewData>()
			: new List<InventorySlotViewData>(slots));
	}

	public static void SetPlayerVitals(PlayerVitalsViewData vitals)
	{
		PlayerVitalsChanged?.Invoke(vitals ?? new PlayerVitalsViewData());
	}

	public static void SetPlayerHealth(int health, int maxHealth = 20)
	{
		SetPlayerVitals(new PlayerVitalsViewData
		{
			Health = health,
			MaxHealth = maxHealth
		});
	}

	public static void SetInteractionPrompt(string text, bool visible)
	{
		InteractionPromptChanged?.Invoke(text ?? "", visible);
	}

	public static void ShowInteractionPrompt(string text)
	{
		SetInteractionPrompt(text, true);
	}

	public static void SetInteractionPromptForNode(string text, bool visible, Node2D target, Vector2 screenOffset)
	{
		InteractionPromptTargetChanged?.Invoke(text ?? "", visible, target, screenOffset);
	}

	public static void ShowInteractionPromptForNode(string text, Node2D target)
	{
		SetInteractionPromptForNode(text, true, target, new Vector2(0, -72));
	}

	public static void ShowInteractionPromptForNodeWithOffset(string text, Node2D target, Vector2 screenOffset)
	{
		SetInteractionPromptForNode(text, true, target, screenOffset);
	}

	public static void HideInteractionPrompt()
	{
		SetInteractionPrompt("", false);
	}

	public static void ShowDialogue(DialogueViewData dialogue)
	{
		DialogueShown?.Invoke(dialogue ?? new DialogueViewData());
	}

	public static void CloseDialogue()
	{
		DialogueClosed?.Invoke();
	}

	public static void ShowToast(string message, ToastKind kind = ToastKind.Info, float seconds = 2.5f)
	{
		ToastRequested?.Invoke(message ?? "", kind, seconds);
	}

	public static void SetPauseMenuVisible(bool visible)
	{
		PauseMenuVisibilityChanged?.Invoke(visible);
	}

	public static void TogglePause()
	{
		PauseToggleRequested?.Invoke();
	}

	public static void QuitGame()
	{
		QuitRequested?.Invoke();
	}
}
