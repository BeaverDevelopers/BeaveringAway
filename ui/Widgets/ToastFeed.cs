using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class ToastFeed : VBoxContainer
{
	readonly List<ToastEntry> _entries = new();

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		AddThemeConstantOverride("separation", 5);
	}

	public override void _Process(double delta)
	{
		for (int i = _entries.Count - 1; i >= 0; i--)
		{
			var entry = _entries[i];
			entry.Remaining -= (float)delta;

			if (entry.Remaining <= 0f)
			{
				RemoveChild(entry.Node);
				entry.Node.QueueFree();
				_entries.RemoveAt(i);
				continue;
			}

			if (entry.Remaining < 0.35f)
			{
				var alpha = Mathf.Clamp(entry.Remaining / 0.35f, 0f, 1f);
				entry.Node.Modulate = new Color(1f, 1f, 1f, alpha);
			}
		}
	}

	public void ShowToast(string message, ToastKind kind = ToastKind.Info, float seconds = 2.5f)
	{
		if (string.IsNullOrEmpty(message))
			return;

		var panel = new PanelContainer
		{
			CustomMinimumSize = new Vector2(260, 0),
			MouseFilter = MouseFilterEnum.Ignore
		};
		panel.AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(ColorFor(kind)));

		var label = UIStyle.MakeLabel(message, 11);
		panel.AddChild(label);

		AddChild(panel);
		MoveChild(panel, 0);

		_entries.Add(new ToastEntry
		{
			Node = panel,
			Remaining = seconds,
			Duration = seconds
		});
	}

	static Color ColorFor(ToastKind kind)
	{
		return kind switch
		{
			ToastKind.Success => UIStyle.RestoreGreen.Darkened(0.35f),
			ToastKind.Warning => UIStyle.Warning.Darkened(0.35f),
			ToastKind.Danger => UIStyle.Danger.Darkened(0.25f),
			_ => UIStyle.Panel
		};
	}

	class ToastEntry
	{
		public Control Node;
		public float Remaining;
		public float Duration;
	}
}
