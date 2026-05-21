using Godot;
using System;
using System.Diagnostics;

public partial class SafeArea : Area2D
{
	[Signal] public delegate void PlayerEnteredSafeAreaEventHandler();

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body.Name == "Player")
		{
			EmitSignal(SignalName.PlayerEnteredSafeArea);
		}


	}
}

