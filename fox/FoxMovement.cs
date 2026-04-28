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
		

	}

	public override void _PhysicsProcess(double delta)
	{
        if (isChasing)
        {
            MoveTowardsPlayer(delta);
        }
		else
        {
            Velocity = Vector2.Zero; //stop moving when not chasing
            // Could add what is should do in "unactive" state here.
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
                Stop(); //stop moving when colliding with player
            }
        }
    
     
	}

    public void Stop()
    {
        isChasing = false; //stop chasing
        Velocity = Vector2.Zero; //stop moving
        GD.Print("Fox stopped moving.");
        
    }
}
