using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class ObjectiveTracker : PanelContainer
{
	[Export] public string HeaderText = "Tracked Goal";
	[Export] public bool StartExpanded;
	[Export] public bool GenerateChildren;
	[Export] public bool ApplyDefaultStyle = true;

	[ExportGroup("Scene Controls")]
	[Export] public Label HeaderLabel;
	[Export] public Button ToggleButton;
	[Export] public Label CurrentTitleLabel;
	[Export] public Label CurrentDetailLabel;
	[Export] public ProgressBar CurrentProgressBar;
	[Export] public Control ExpandedPanel;
	[Export] public VBoxContainer Rows;

	readonly List<ObjectiveViewData> _objectives = new();
	string _trackedObjectiveId = "";
	bool _built;
	bool _expanded;

	public override void _Ready()
	{
		_expanded = StartExpanded;
		BindSceneNodes();
		if (GenerateChildren)
			Build();
		ApplyStyle();
		WireToggleButton();
		RefreshExpandedVisibility();
	}

	public void Build()
	{
		if (_built)
			return;

		_built = true;
		MouseFilter = MouseFilterEnum.Pass;
		ApplyStyle();

		var root = new VBoxContainer
		{
			Name = "ObjectiveRoot",
			CustomMinimumSize = new Vector2(290, 0),
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddThemeConstantOverride("separation", 6);
		AddChild(root);

		var headerRow = new HBoxContainer
		{
			Name = "HeaderRow",
			MouseFilter = MouseFilterEnum.Ignore
		};
		headerRow.AddThemeConstantOverride("separation", 8);
		root.AddChild(headerRow);

		HeaderLabel = UIStyle.MakeLabel(HeaderText, 14);
		HeaderLabel.Name = "HeaderLabel";
		HeaderLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		headerRow.AddChild(HeaderLabel);

		ToggleButton = new Button
		{
			Name = "ToggleButton",
			Text = "+"
		};
		UIStyle.ApplyButton(ToggleButton);
		headerRow.AddChild(ToggleButton);

		CurrentTitleLabel = UIStyle.MakeLabel("No tracked goal", 12);
		CurrentTitleLabel.Name = "CurrentTitleLabel";
		root.AddChild(CurrentTitleLabel);

		CurrentDetailLabel = UIStyle.MakeLabel("", 10, true);
		CurrentDetailLabel.Name = "CurrentDetailLabel";
		root.AddChild(CurrentDetailLabel);

		CurrentProgressBar = new ProgressBar
		{
			Name = "CurrentProgressBar",
			CustomMinimumSize = new Vector2(240, 8),
			MinValue = 0,
			MaxValue = 100
		};
		UIStyle.ApplyProgressBar(CurrentProgressBar, UIStyle.WaterBlue);
		root.AddChild(CurrentProgressBar);

		ExpandedPanel = new VBoxContainer
		{
			Name = "ExpandedPanel",
			MouseFilter = MouseFilterEnum.Ignore
		};
		root.AddChild(ExpandedPanel);

		Rows = new VBoxContainer
		{
			Name = "Rows",
			MouseFilter = MouseFilterEnum.Ignore
		};
		Rows.AddThemeConstantOverride("separation", 5);
		ExpandedPanel.AddChild(Rows);

		WireToggleButton();
		RefreshExpandedVisibility();
	}

	public void SetObjectives(IReadOnlyList<ObjectiveViewData> objectives)
	{
		EnsureControls();
		_objectives.Clear();

		if (objectives != null)
		{
			foreach (var objective in objectives)
				_objectives.Add(objective);
		}

		var tracked = ResolveTrackedObjective();
		UpdateTrackedGoal(tracked);
		RebuildRows();
	}

	void UpdateTrackedGoal(ObjectiveViewData tracked)
	{
		if (tracked == null)
		{
			if (CurrentTitleLabel != null)
				CurrentTitleLabel.Text = "No tracked goal";
			if (CurrentDetailLabel != null)
				CurrentDetailLabel.Text = "";
			if (CurrentProgressBar != null)
				CurrentProgressBar.Value = 0;
			return;
		}

		_trackedObjectiveId = tracked.ObjectiveId;

		if (CurrentTitleLabel != null)
			CurrentTitleLabel.Text = tracked.IsComplete ? "[done] " + tracked.Title : tracked.Title;
		if (CurrentDetailLabel != null)
			CurrentDetailLabel.Text = tracked.Detail;
		if (CurrentProgressBar != null)
		{
			CurrentProgressBar.Value = tracked.Progress * 100.0;
			if (ApplyDefaultStyle)
				UIStyle.ApplyProgressBar(CurrentProgressBar, tracked.IsComplete ? UIStyle.RestoreGreen : UIStyle.WaterBlue);
		}
	}

	void RebuildRows()
	{
		if (Rows == null)
			return;

		ClearRows();

		if (_objectives.Count == 0)
		{
			var empty = UIStyle.MakeLabel("No goals yet", 11, true);
			Rows.AddChild(empty);
			return;
		}

		foreach (var objective in _objectives)
			AddObjectiveRow(objective);
	}

	void AddObjectiveRow(ObjectiveViewData objective)
	{
		var row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		row.AddThemeConstantOverride("separation", 6);

		var textRoot = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		textRoot.AddThemeConstantOverride("separation", 2);
		row.AddChild(textRoot);

		var titleText = objective.IsComplete ? "[done] " + objective.Title : objective.Title;
		var title = UIStyle.MakeLabel(titleText, 11, objective.State == ObjectiveState.Locked);
		textRoot.AddChild(title);

		var progress = new ProgressBar
		{
			CustomMinimumSize = new Vector2(160, 7),
			MinValue = 0,
			MaxValue = 100,
			Value = objective.Progress * 100.0
		};
		UIStyle.ApplyProgressBar(progress, objective.IsComplete ? UIStyle.RestoreGreen : UIStyle.WaterBlue);
		textRoot.AddChild(progress);

		var trackButton = new Button
		{
			Text = objective.ObjectiveId == _trackedObjectiveId ? "Tracked" : "Track",
			Disabled = objective.ObjectiveId == _trackedObjectiveId
		};
		UIStyle.ApplyButton(trackButton);
		trackButton.Pressed += () =>
		{
			_trackedObjectiveId = objective.ObjectiveId;
			UIEventBus.RequestObjectiveTracking(_trackedObjectiveId);
			UpdateTrackedGoal(objective);
			RebuildRows();
		};
		row.AddChild(trackButton);

		Rows.AddChild(row);
	}

	ObjectiveViewData ResolveTrackedObjective()
	{
		foreach (var objective in _objectives)
		{
			if (objective.IsTracked)
				return objective;
		}

		if (!string.IsNullOrEmpty(_trackedObjectiveId))
		{
			foreach (var objective in _objectives)
			{
				if (objective.ObjectiveId == _trackedObjectiveId)
					return objective;
			}
		}

		foreach (var objective in _objectives)
		{
			if (objective.State == ObjectiveState.Active)
				return objective;
		}

		return _objectives.Count > 0 ? _objectives[0] : null;
	}

	void ClearRows()
	{
		foreach (Node child in Rows.GetChildren())
		{
			Rows.RemoveChild(child);
			child.QueueFree();
		}
	}

	void ToggleExpanded()
	{
		_expanded = !_expanded;
		RefreshExpandedVisibility();
	}

	void RefreshExpandedVisibility()
	{
		if (ExpandedPanel != null)
			ExpandedPanel.Visible = _expanded;
		else if (Rows != null)
			Rows.Visible = _expanded;

		if (ToggleButton != null)
			ToggleButton.Text = _expanded ? "-" : "+";
	}

	void EnsureControls()
	{
		BindSceneNodes();
		if (CurrentTitleLabel == null && GenerateChildren)
			Build();
	}

	void BindSceneNodes()
	{
		HeaderLabel ??= FindChild("HeaderLabel", true, false) as Label;
		ToggleButton ??= FindChild("ToggleButton", true, false) as Button;
		CurrentTitleLabel ??= FindChild("CurrentTitleLabel", true, false) as Label;
		CurrentDetailLabel ??= FindChild("CurrentDetailLabel", true, false) as Label;
		CurrentProgressBar ??= FindChild("CurrentProgressBar", true, false) as ProgressBar;
		ExpandedPanel ??= FindChild("ExpandedPanel", true, false) as Control;
		Rows ??= FindChild("Rows", true, false) as VBoxContainer;

		if (HeaderLabel != null)
		{
			HeaderLabel.Text = HeaderText;
			if (ApplyDefaultStyle)
				UIStyle.ApplyLabel(HeaderLabel, 14);
		}
		if (CurrentTitleLabel != null && ApplyDefaultStyle)
			UIStyle.ApplyLabel(CurrentTitleLabel, 12);
		if (CurrentDetailLabel != null && ApplyDefaultStyle)
			UIStyle.ApplyLabel(CurrentDetailLabel, 10, true);
		if (CurrentProgressBar != null && ApplyDefaultStyle)
			UIStyle.ApplyProgressBar(CurrentProgressBar, UIStyle.WaterBlue);
		if (ToggleButton != null && ApplyDefaultStyle)
			UIStyle.ApplyButton(ToggleButton);
	}

	void WireToggleButton()
	{
		if (ToggleButton == null || ToggleButton.HasMeta("objective_toggle_wired"))
			return;

		ToggleButton.SetMeta("objective_toggle_wired", true);
		ToggleButton.Pressed += ToggleExpanded;
	}

	void ApplyStyle()
	{
		if (ApplyDefaultStyle)
			AddThemeStyleboxOverride("panel", UIStyle.MakePanelStyle(UIStyle.Panel));
	}
}
