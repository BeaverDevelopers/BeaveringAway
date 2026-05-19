using Godot;
using System;

public partial class FoxMovement : CharacterBody2D
{
    // Enums for fox states
    private enum FoxState
    {
        Chasing,
        Threatened,
        Escaping
    }

    public float speed = 220f;
    CharacterBody2D player;
    private FoxState _foxState = FoxState.Chasing;
    private DeerMovement _interactingDeer; // The deer this fox is interacting with
    private float _threatTimer = 0f;
    private const float THREAT_DURATION = 2f;
    private Vector2 _lastDirection = Vector2.Down;
    private string _lastAnimationPlayed = ""; // Track last animation to prevent flickering

    [Export] public float DeerDetectionDistance = 400f;
    [Export] public float DeerEscapeDistance = 500f;

    private AnimatedSprite2D _animSprite;

    public override void _Ready()
    {
        player = GetNode<CharacterBody2D>("../../Player");

        _animSprite = GetNode<AnimatedSprite2D>("Sprite2D/AnimatedSprite2D");

        // Add to fox group for deer detection
        AddToGroup("fox");

        var onscreen = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        onscreen.ScreenExited += Destroy;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Update deer interaction state
        UpdateDeerInteraction();

        // Handle fox behavior based on state
        switch (_foxState)
        {
            case FoxState.Chasing:
                MoveTowardsPlayer(delta);
                break;
            case FoxState.Threatened:
                HandleThreatened(delta);
                break;
            case FoxState.Escaping:
                HandleEscaping(delta);
                break;
        }

        UpdateAnimation();
    }

    public void MoveTowardsPlayer(double delta)
    {
        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = direction * speed;
        _lastDirection = direction;
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
                    _foxState = FoxState.Escaping;
                }
            }
        }
    }

    private void UpdateDeerInteraction()
    {
        // If not interacting with a deer, try to find one
        if (_interactingDeer == null)
        {
            // Find nearby deer
            float closestDeerDistance = DeerDetectionDistance;
            DeerMovement nearestDeer = null;

            var deerNodes = GetTree().GetNodesInGroup("deer");
            foreach (var deerNode in deerNodes)
            {
                if (deerNode is DeerMovement deer && !deer.IsQueuedForDeletion())
                {
                    float distance = GlobalPosition.DistanceTo(deer.GlobalPosition);
                    if (distance < closestDeerDistance)
                    {
                        nearestDeer = deer;
                        closestDeerDistance = distance;
                    }
                }
            }

            // If found a nearby deer and currently chasing player, switch to deer threat
            // But NOT if we're already escaping (e.g., just stole something)
            if (nearestDeer != null && _foxState == FoxState.Chasing)
            {
                _interactingDeer = nearestDeer;
                _foxState = FoxState.Threatened;
                _threatTimer = 0f;
                GD.Print($"Fox detected deer at distance {closestDeerDistance:F1}, entering threatened state");
                return;
            }
        }

        // If already interacting with a deer, only check if it's deleted
        if (_interactingDeer != null && _interactingDeer.IsQueuedForDeletion())
        {
            _interactingDeer = null;
            // Don't change state here - if in Escaping, continue escaping from player
            // Only go back to Chasing if we're in Threatened state
            if (_foxState == FoxState.Threatened)
            {
                _foxState = FoxState.Chasing;
                GD.Print("Fox: Deer deleted, resuming player chase");
            }
        }
    }

    private void HandleThreatened(double delta)
    {
        if (_interactingDeer == null)
        {
            _foxState = FoxState.Chasing;
            GD.Print("Fox: Deer is null in threatened state, resuming chase");
            return;
        }

        // Face towards the deer (for animation) but retreat slowly
        Vector2 directionToDeer = (_interactingDeer.GlobalPosition - GlobalPosition).Normalized();
        _lastDirection = directionToDeer; // Face towards deer
        _lastAnimationPlayed = ""; // Reset to force animation update

        // Move away from deer slowly (retreat)
        Vector2 retreatDirection = -directionToDeer;
        Velocity = retreatDirection * (speed * 0.3f); // Slow retreat
        MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);

        // Update threat timer - stay in threatened state for THREAT_DURATION
        _threatTimer += (float)delta;
        GD.Print($"Fox threatened state: {_threatTimer:F2}s / {THREAT_DURATION}s");

        if (_threatTimer >= THREAT_DURATION)
        {
            // Now transition to escaping
            _foxState = FoxState.Escaping;
            GD.Print("Fox threat duration ended, transitioning to escape");
        }
    }

    private void HandleEscaping(double delta)
    {
        // If escaping because of deer, move away from deer
        if (_interactingDeer != null && !_interactingDeer.IsQueuedForDeletion())
        {
            // Escape from deer - move away and animate in escape direction
            Vector2 directionToDeer = (_interactingDeer.GlobalPosition - GlobalPosition).Normalized();
            Vector2 escapeDirection = -directionToDeer;
            _lastDirection = escapeDirection; // Animate in escape direction
            _lastAnimationPlayed = ""; // Reset to force animation update

            Velocity = escapeDirection * speed; // Full speed escape
            MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
        }
        else
        {
            // Escaping from player (stole something), move away from player
            Vector2 directionFromPlayer = (GlobalPosition - player.GlobalPosition).Normalized();
            _lastDirection = directionFromPlayer;
            _lastAnimationPlayed = ""; // Reset to force animation update

            Velocity = directionFromPlayer * (speed * 0.8f);
            MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
        }
    }

    public void WalkAway(double delta)
    {
        _foxState = FoxState.Escaping;
        Vector2 direction = (GlobalPosition - player.GlobalPosition).Normalized();
        Velocity = direction * (speed * 0.8f);
        _lastDirection = direction;
        MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
    }

    // Called by DeerMovement when deer is threatening
    public void NotifyDeerThreat(DeerMovement deer)
    {
        // Only interact if not already interacting with another deer
        if (_interactingDeer == null && _foxState == FoxState.Chasing)
        {
            _interactingDeer = deer;
            _foxState = FoxState.Threatened;
            _threatTimer = 0f;
            GD.Print("Fox notified of deer threat, entering threatened state");
        }
    }

    // Called by DeerMovement when deer starts chasing
    public void NotifyDeerChasing(DeerMovement deer)
    {
        // Only respond if this is the deer we're interacting with
        if (_interactingDeer == deer && _foxState == FoxState.Threatened)
        {
            _foxState = FoxState.Escaping;
            _threatTimer = 0f;
            GD.Print("Fox notified of deer chase, entering escape state");
        }
    }

    private void UpdateAnimation()
    {
        if (Velocity.Length() < 1f && _foxState != FoxState.Threatened)
        {
            _animSprite.Stop();
            _lastAnimationPlayed = "";
            return;
        }

        // Use last direction for animation
        Vector2 animDirection = Velocity.Length() > 1f ? Velocity : _lastDirection;

        float x = animDirection.X;
        float y = animDirection.Y;

        string animName = "";
        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            // Horizontal movement
            if (x > 0)
            {
                animName = "walk_right";
            }
            else
            {
                // Use walk_right but flip it for left
                animName = "walk_left";
            }
        }
        else
        {
            // Vertical movement
            animName = y > 0 ? "walk_down" : "walk_up";
        }

        // Only play animation if it's different from the last one
        if (animName != _lastAnimationPlayed)
        {
            if (animName == "walk_left")
            {
                // If walk_left doesn't exist, use walk_right with flip
                if (_animSprite.SpriteFrames.HasAnimation("walk_left"))
                {
                    _animSprite.Play("walk_left");
                }
                else
                {
                    _animSprite.Play("walk_right");
                    _animSprite.FlipH = true;
                }
            }
            else
            {
                if (animName == "walk_right")
                {
                    _animSprite.FlipH = false;
                }
                _animSprite.Play(animName);
            }
            _lastAnimationPlayed = animName;
        }
    }

    public void Destroy()
    {
        // Allow destruction in any state when going off-screen
        QueueFree();
        GD.Print($"Fox destroyed (was in {_foxState} state).");
    }
}