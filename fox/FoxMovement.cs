using Godot;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

public partial class FoxMovement : CharacterBody2D
{
    // Enums for fox states
    private enum FoxState
    {
        Chasing,
        Threatened,
        WaitingAtShore,
        Escaping
    }

    public float speed = 220f;
    CharacterBody2D player;
    private PlayerMove _playerMove;
    private Game _game;
    private FoxState _foxState = FoxState.Chasing;
    private DeerMovement _interactingDeer; // The deer this fox is interacting with
    private float _threatTimer = 0f;
    private float _shoreWaitTimer = 0f;
    private const float THREAT_DURATION = 2f;
    [Export] public float ShoreWaitDuration = 3f;
    private Vector2 _lastDirection = Vector2.Down;
    private string _lastAnimationPlayed = ""; // Track last animation to prevent flickering

    [Export] public float DeerDetectionDistance = 400f;
    [Export] public float DeerEscapeDistance = 500f;

    private AnimatedSprite2D _animSprite;

    //for hut as safe place
    bool hutPlaced = false;
    bool playerIsSafe = false;

    public override void _Ready()
    {
        player = GetNode<CharacterBody2D>("../../world/Player");
        _playerMove = player as PlayerMove;
        _game = GetNode<Game>("../..");

        _animSprite = GetNode<AnimatedSprite2D>("Sprite2D/AnimatedSprite2D");

        // Add to fox group for deer detection
        AddToGroup("fox");

        var onscreen = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        onscreen.ScreenExited += Destroy;
    }

    public override void _Process(double delta)
    {
        //To know when the hut is placed so we can start waiting for the signal
        if (!hutPlaced)
        {
            var world = GetTree().CurrentScene.GetNode("world");
            var safeArea = world.GetNodeOrNull<SafeArea>("hut/SafeArea");
            if (safeArea != null)
            {
            Debug.WriteLine("Hut is placed, beaver has a safe area");
            hutPlaced = true;
            safeArea.PlayerEnteredSafeArea += PlayerEnteredSafeArea;
            }
        }

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
            case FoxState.WaitingAtShore:
                HandleWaitingAtShore(delta);
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
        _lastDirection = direction;

        if (!CanReachPlayer())
        {
            BeginWaitingAtShore();
            return;
        }

        Vector2 nextPosition = GlobalPosition + direction * speed * (float)delta;
        if (IsInWater(nextPosition))
        {
            BeginWaitingAtShore();
            return;
        }

        Velocity = direction * speed;
        var collision = MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
        if (collision != null)
        {
            var collider = collision.GetCollider();
            if (collider == player && TryStealFromPlayer())
                _foxState = FoxState.Escaping;
        }
    }

    private bool TryStealFromPlayer()
    {
        if (!InventoryData.TryStealRandomItem(out var stolen))
            return false;

        GD.Print($"Fox stole {stolen.DisplayName} (id {stolen.ItemId})");
        _playerMove?.PlayFoxStealFeedback(stolen.Icon);
        UIEventBus.ShowToast($"Fox stole {stolen.DisplayName}!", ToastKind.Warning);
        return true;
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
        TryMove(Velocity * (float)delta);

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
            TryMove(Velocity * (float)delta);
        }
        else
        {
            // Escaping from player (stole something), move away from player
            Vector2 directionFromPlayer = (GlobalPosition - player.GlobalPosition).Normalized();
            _lastDirection = directionFromPlayer;
            _lastAnimationPlayed = ""; // Reset to force animation update

            Velocity = directionFromPlayer * (speed * 0.8f);
            TryMove(Velocity * (float)delta);
        }
    }

    private void HandleWaitingAtShore(double delta)
    {
        if (CanReachPlayer())
        {
            _foxState = FoxState.Chasing;
            _shoreWaitTimer = 0f;
            GD.Print("Fox: Player is reachable again, resuming chase");
            return;
        }

        _lastDirection = (player.GlobalPosition - GlobalPosition).Normalized();
        _lastAnimationPlayed = "";
        Velocity = Vector2.Zero;

        _shoreWaitTimer += (float)delta;
        if (_shoreWaitTimer >= ShoreWaitDuration)
        {
            _foxState = FoxState.Escaping;
            GD.Print("Fox gave up waiting at shore, escaping");
        }
    }

    private void BeginWaitingAtShore()
    {
        if (_foxState != FoxState.WaitingAtShore)
        {
            _shoreWaitTimer = 0f;
            GD.Print("Fox stopped at shore or hut, waiting for player");
        }

        _foxState = FoxState.WaitingAtShore;
        Velocity = Vector2.Zero;
        _lastDirection = (player.GlobalPosition - GlobalPosition).Normalized();
    }

    private bool CanReachPlayer()
    {
        Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
        if (toPlayer.LengthSquared() < 1f)
            return true;

        Vector2 direction = toPlayer.Normalized();
        float checkDistance = Mathf.Min(toPlayer.Length(), 96f);
        int steps = Mathf.Max(1, (int)(checkDistance / 16f));
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            if (IsInWater(GlobalPosition + direction * checkDistance * t))
                return false;

            if (playerIsSafe) // if player is in hut/safe area
                return false;
        }

        return true;
    }

    private bool IsInWater(Vector2 globalPosition)
    {
        return _game != null && _game.IsPositionInWater(globalPosition);
    }

    private void TryMove(Vector2 motion)
    {
        if (IsInWater(GlobalPosition + motion))
        {
            Velocity = Vector2.Zero;
            return;
        }

        MoveAndCollide(motion, false, 0.08f, true);
    }

    public void WalkAway(double delta)
    {
        _foxState = FoxState.Escaping;
        Vector2 direction = (GlobalPosition - player.GlobalPosition).Normalized();
        Velocity = direction * (speed * 0.8f);
        _lastDirection = direction;
        MoveAndCollide(Velocity * (float)delta, false, (float)0.08, true);
    }

    //Signal from Area2d from hut that the player has entered hut area
    private void PlayerEnteredSafeArea()
    {
        Debug.WriteLine("Beaver is safe close to the hut");
        playerIsSafe = true;
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
        if (Velocity.Length() < 1f && _foxState != FoxState.Threatened && _foxState != FoxState.WaitingAtShore)
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