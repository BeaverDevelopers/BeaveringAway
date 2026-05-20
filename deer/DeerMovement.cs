using Godot;
using System;

public partial class DeerMovement : CharacterBody2D
{
	// Enums for state management
	public enum DeerState
	{
		Wandering,
		Threatened,
		Chasing
	}

	public enum Gender
	{
		Male,
		Female
	}

	// Movement and behavior parameters
	[Export] public float WanderSpeed = 60f;
	[Export] public float ChasingSpeed = 150f;
	[Export] public float WanderRange = 200f;
	[Export] public float FoxDetectionDistance = 300f;
	[Export] public float FoxThreatenedDistance = 150f;
	[Export] public float DespawnDistance = 600f;
	[Export] public float WanderChangeInterval = 3f;

	// State variables
	private DeerState _currentState = DeerState.Wandering;
	private Gender _gender;
	private AnimatedSprite2D _animSprite;
	private CharacterBody2D _player;
	private FoxMovement _targetFox;
	private Vector2 _startPosition;
	private Vector2 _wanderDirection = Vector2.Right;
	private float _wanderChangeTimer = 0f;
	private float _threatTimer = 0f;
	private const float THREAT_DURATION = 2f;
	private Vector2 _lastDirection = Vector2.Down;
	private string _lastAnimationPlayed = ""; // Track last animation to prevent flickering

	public override void _Ready()
	{
		// Get references
		_player = GetNode<CharacterBody2D>("../../world/Player");
		_animSprite = GetNode<AnimatedSprite2D>("Sprite2D/AnimatedSprite2D");

		// Randomize gender
		_gender = (Gender)(GD.Randi() % 2);

		// Store starting position
		_startPosition = GlobalPosition;

		// Setup despawn detection
		var onscreen = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		onscreen.ScreenExited += OnScreenExited;

		// Random initial wander direction
		RandomizeWanderDirection();
	}

	public override void _PhysicsProcess(double delta)
	{
		// Check if player is too far away (despawn)
		if (GlobalPosition.DistanceTo(_player.GlobalPosition) > DespawnDistance)
		{
			QueueFree();
			return;
		}

		// Update state based on fox detection
		UpdateFoxInteraction();

		// Update behavior based on state
		switch (_currentState)
		{
			case DeerState.Wandering:
				UpdateWandering((float)delta);
				break;
			case DeerState.Threatened:
				UpdateThreatened((float)delta);
				break;
			case DeerState.Chasing:
				UpdateChasing((float)delta);
				break;
		}

		UpdateAnimation();
	}

	private void UpdateFoxInteraction()
	{
		// If already threatening/chasing a fox, keep that one
		if (_targetFox != null)
		{
			if (_targetFox.IsQueuedForDeletion())
			{
				_targetFox = null;
				_currentState = DeerState.Wandering;
				GD.Print("Deer: Fox deleted, returning to wandering");
				return;
			}

			// Already interacting, stay in current state - don't check distance
			// Continue with threat or chase until fox is deleted
			return;
		}

		// Not interacting with any fox, look for one
		_targetFox = null;
		float closestFoxDistance = FoxThreatenedDistance;

		var foxNodes = GetTree().GetNodesInGroup("fox");
		foreach (var foxNode in foxNodes)
		{
			if (foxNode is FoxMovement fox && !fox.IsQueuedForDeletion())
			{
				float distance = GlobalPosition.DistanceTo(fox.GlobalPosition);
				if (distance < closestFoxDistance)
				{
					_targetFox = fox;
					closestFoxDistance = distance;
				}
			}
		}

		// Start interaction with closest fox if detected and in wandering state
		if (_targetFox != null && _currentState == DeerState.Wandering)
		{
			_currentState = DeerState.Threatened;
			_threatTimer = 0f;
			_targetFox.NotifyDeerThreat(this);
			GD.Print($"Deer detected fox at distance {closestFoxDistance:F1}, entering threatened state");
		}
	}

	private void UpdateWandering(float delta)
	{
		// Update wander direction timer
		_wanderChangeTimer += delta;
		if (_wanderChangeTimer >= WanderChangeInterval)
		{
			RandomizeWanderDirection();
			_wanderChangeTimer = 0f;
		}

		// Move in wander direction
		Velocity = _wanderDirection * WanderSpeed;
		_lastDirection = _wanderDirection;
		MoveAndCollide(Velocity * delta, false, 0.08f, true);
	}

	private void UpdateThreatened(float delta)
	{
		if (_targetFox == null)
		{
			_currentState = DeerState.Wandering;
			return;
		}

		// Face the fox (stand still and face threat direction)
		Vector2 directionToFox = (_targetFox.GlobalPosition - GlobalPosition).Normalized();
		_lastDirection = directionToFox;
		_lastAnimationPlayed = ""; // Reset to force animation update

		// Stand still and face the fox
		Velocity = Vector2.Zero;

		// Update threat timer
		_threatTimer += (float)delta;
		GD.Print($"Deer threatened state: {_threatTimer:F2}s / {THREAT_DURATION}s");

		if (_threatTimer >= THREAT_DURATION)
		{
			// Transition to chasing after threat duration
			_currentState = DeerState.Chasing;
			GD.Print("Deer threat duration ended, transitioning to chase");
			_targetFox.NotifyDeerChasing(this);
		}
	}

	private void UpdateChasing(float delta)
	{
		if (_targetFox == null || _targetFox.IsQueuedForDeletion())
		{
			_currentState = DeerState.Wandering;
			GD.Print("Deer: Fox deleted or null, returning to wandering");
			return;
		}

		// Chase the fox
		Vector2 directionToFox = (_targetFox.GlobalPosition - GlobalPosition).Normalized();
		Velocity = directionToFox * ChasingSpeed;
		_lastDirection = directionToFox;
		_lastAnimationPlayed = ""; // Reset to force animation update
		MoveAndCollide(Velocity * delta, false, 0.08f, true);

		float distance = GlobalPosition.DistanceTo(_targetFox.GlobalPosition);
		GD.Print($"Deer chasing fox, distance: {distance:F1}");

		// Check if fox is still in range
		if (distance > FoxDetectionDistance * 1.5f)
		{
			_currentState = DeerState.Wandering;
			GD.Print("Deer: Fox too far away, returning to wandering");
		}
	}

	private void UpdateAnimation()
	{
		if (Velocity.Length() < 1f && _currentState != DeerState.Threatened)
		{
			_animSprite.Stop();
			_lastAnimationPlayed = "";
			return;
		}

		// Use last direction or current velocity for animation
		Vector2 animDirection = Velocity.Length() > 1f ? Velocity : _lastDirection;

		float x = animDirection.X;
		float y = animDirection.Y;

		// Determine animation based on direction
		string baseAnimName = "";
		bool flipH = false;

		if (Mathf.Abs(x) > Mathf.Abs(y))
		{
			// Horizontal movement
			if (x > 0)
			{
				baseAnimName = "walk_right";
				flipH = false;
			}
			else
			{
				// Use walk_right but flip for left
				baseAnimName = "walk_right";
				flipH = true;
			}
		}
		else
		{
			// Vertical movement
			baseAnimName = y > 0 ? "walk_down" : "walk_up";
			flipH = false;
		}

		// Try with gender suffix first
		string animName = $"{baseAnimName}_{(_gender == Gender.Male ? "male" : "female")}";

		// Only play animation if it's different from the last one (prevent flickering)
		if (animName != _lastAnimationPlayed)
		{
			// Try gender-specific animation first
			if (_animSprite.SpriteFrames.HasAnimation(animName))
			{
				_animSprite.Play(animName);
			}
			// Fall back to base animation name
			else if (_animSprite.SpriteFrames.HasAnimation(baseAnimName))
			{
				_animSprite.Play(baseAnimName);
			}
			_lastAnimationPlayed = animName;
		}

		// Apply flip for left-facing animation (if using walk_right flipped)
		if (baseAnimName == "walk_right")
		{
			_animSprite.FlipH = flipH;
		}
		else
		{
			_animSprite.FlipH = false;
		}
	}

	private void RandomizeWanderDirection()
	{
		float angle = (float)(GD.Randf() * Mathf.Tau);
		_wanderDirection = Vector2.FromAngle(angle);
	}

	private void OnScreenExited()
	{
		QueueFree();
		GD.Print($"Deer despawned (off-screen, was in {_currentState} state).");
	}

	// Called by FoxMovement when fox is threatened
	public void NotifyFoxThreatened()
	{
		// This allows fox to know when deer detects it
	}

	// Called by FoxMovement when fox escapes
	public void NotifyFoxEscaped()
	{
		if (_currentState == DeerState.Chasing)
		{
			_currentState = DeerState.Wandering;
		}
	}

	// Public property to get current state (for fox to check)
	public DeerState CurrentState => _currentState;

	// Property to check if deer is threatening
	public bool IsThreatening => _currentState == DeerState.Threatened;

	// Property to check if deer is chasing
	public bool IsChasing => _currentState == DeerState.Chasing;
}
