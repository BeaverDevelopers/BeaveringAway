using Godot;
using System;
using System.ComponentModel;
using System.Diagnostics;

public partial class FoxMovement : CharacterBody2D
{
	public float speed = 100f;

	CharacterBody2D player;

    bool isChasing = true; // whether the fox should chase the player


	public override void _Ready()
	{
		player = GetNode<CharacterBody2D>("../player"); //get reference to player
		var onscreen = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        var visibility = onscreen.Connect("on_screen_exited", new Callable(this, nameof(Destroy)));

	}

	public override void _PhysicsProcess(double delta)
	{
        if (isChasing)
        {
            MoveTowardsPlayer(delta);
        }
		else
        {
            WalkAway(delta);
        }
        
	}

	public void MoveTowardsPlayer(double delta)
	{
		LookAt(player.GlobalPosition); //face player
		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized(); //get direction to player
		Velocity = direction * speed; //set velocity towards player
        var collision = MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
        if (collision != null)
        {
            var collider = collision.GetCollider();
            if (collider == player)
            {
                GD.Print("Fox collided with player!");
                InventoryData.AddItem(1,-1); //steals one stick
                GD.Print("Fox stole a stick!");
                WalkAway(delta); //walk away from player after collision
            }
        }
    
     
	}

    public void WalkAway(double delta)
    {
        isChasing = false; //stop chasing
        Vector2 direction = (GlobalPosition - player.GlobalPosition).Normalized(); //get direction from player
		Velocity = direction * (speed * 0.5f); //set velocity away from player
        var collision = MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
        
    }

    public void Destroy()
    {
        //This never seems to be called.
        QueueFree(); //remove fox from scene
        GD.Print("Fox destroyed.");
    }
}
