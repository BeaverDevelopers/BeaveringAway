using Godot;
using System;

public partial class FoxMovement : CharacterBody2D
{
    public float speed = 220f;
    CharacterBody2D player;
    bool isChasing = true;


    private AnimatedSprite2D _animSprite;

    public override void _Ready()
    {
        player = GetNode<CharacterBody2D>("../../Player");

        _animSprite = GetNode<AnimatedSprite2D>("Sprite2D/AnimatedSprite2D");


        var onscreen = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        onscreen.ScreenExited += Destroy;
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


        UpdateAnimation();
    }

    public void MoveTowardsPlayer(double delta)
    {

        // LookAt(player.GlobalPosition);

        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = direction * speed;
        var collision = MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
        if (collision != null)
        {
            var collider = collision.GetCollider();
            if (collider == player)
            {
                
                if (InventoryData.Count > 0)
                {                
                    InventoryData.AddItem(1, -1);
                    GD.Print("Fox stole a stick!");
                    WalkAway(delta);
                }
                //If you don't have anything in your inventory the fox will keep following you until you pick something up
                // Could do an else statement, that it maybe steals a log from the ground
            }
        }
    }

    public void WalkAway(double delta)
    {
        isChasing = false;
        Vector2 direction = (GlobalPosition - player.GlobalPosition).Normalized();
        Velocity = direction * (speed * 0.8f);
        MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
    }


    private void UpdateAnimation()
    {

        if (Velocity.Length() < 1f)
        {
            _animSprite.Stop();
            return;
        }

        float x = Velocity.X;
        float y = Velocity.Y;


        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            _animSprite.Play(x > 0 ? "walk_right" : "walk_left");
        }

        else
        {
            _animSprite.Play(y > 0 ? "walk_down" : "walk_up");
        }
    }

    public void Destroy()
    {
        if (!isChasing)
        {
            QueueFree();
            GD.Print("Fox destroyed.");
        }
    }
}