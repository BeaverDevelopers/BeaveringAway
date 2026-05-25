using Godot;
using System;
using System.Collections.Generic;

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
	[Export] public float RestMinDuration = 5f;
	[Export] public float RestMaxDuration = 14f;
	[Export] public float MoveMinDuration = 0.8f;
	[Export] public float MoveMaxDuration = 2.5f;

	// State variables
	private DeerState _currentState = DeerState.Wandering;
	private Gender _gender;
	private AnimatedSprite2D _animSprite;
	private CharacterBody2D _player;
	private FoxMovement _targetFox;
	private Game _game;
	private Vector2 _startPosition;
	private Vector2 _wanderDirection = Vector2.Right;
	private bool _isResting = true;
	private float _phaseTimer;
	private float _threatTimer = 0f;
	private const float THREAT_DURATION = 2f;
	private Vector2 _lastDirection = Vector2.Down;
	private string _lastAnimationPlayed = ""; // Track last animation to prevent flickering

	// Herd-related variables
	private int _herdId = -1; // -1 means not part of a herd
	private DeerMovement _herdMale; // Reference to the male in the herd (for females)
	private List<DeerMovement> _herdFemales = new(); // List of females in herd (for male)
	[Export] public float HerdFreeRange = 100f;

	public override void _Ready()
	{
		// Get references
		_player = GetNode<CharacterBody2D>("../Player");
		_animSprite = GetNode<AnimatedSprite2D>("Sprite2D/AnimatedSprite2D");
		_game = GetParent()?.GetParent() as Game;

		// Store starting position
		_startPosition = GlobalPosition;

		RandomizeWanderDirection();
		_phaseTimer = (float)GD.RandRange(0f, RestMaxDuration);
		_isResting = true;

		if (IsInHerd)
			ApplyGenderAnimation(_lastDirection, play: false);
	}

	public override void _PhysicsProcess(double delta)
	{
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
		if (!IsInHerd)
			return;

		// Females follow the herd male; males roam freely
		if (IsHerdFemale)
		{
			UpdateHerdFemaleWandering(delta);
			return;
		}

		UpdateActivityPhase(delta);
		if (_isResting)
		{
			Velocity = Vector2.Zero;
			return;
		}

		TryMoveInDirection(_wanderDirection, WanderSpeed, delta);

		if (IsHerdMale)
			_herdFemales.RemoveAll(f => f == null);
	}

	private void UpdateHerdFemaleWandering(float delta)
	{
		if (_herdMale == null)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 directionToMale = (_herdMale.GlobalPosition - GlobalPosition).Normalized();
		float distanceToMale = GlobalPosition.DistanceTo(_herdMale.GlobalPosition);

		if (distanceToMale > HerdFreeRange)
		{
			TryMoveInDirection(directionToMale, WanderSpeed, delta);
			return;
		}

		UpdateActivityPhase(delta);
		if (_isResting)
		{
			Velocity = Vector2.Zero;
			return;
		}

		TryMoveInDirection(_wanderDirection, WanderSpeed * 0.55f, delta);
	}

	private void UpdateActivityPhase(float delta)
	{
		_phaseTimer -= delta;
		if (_phaseTimer > 0f)
			return;

		if (_isResting)
			BeginMovePhase();
		else
			BeginRestPhase();
	}

	private void BeginRestPhase()
	{
		_isResting = true;
		_phaseTimer = (float)GD.RandRange(RestMinDuration, RestMaxDuration);
		Velocity = Vector2.Zero;
	}

	private void BeginMovePhase()
	{
		_isResting = false;
		_phaseTimer = (float)GD.RandRange(MoveMinDuration, MoveMaxDuration);
		RandomizeWanderDirection();
	}

	private void TryMoveInDirection(Vector2 direction, float speed, float delta)
	{
		Vector2 nextPosition = GlobalPosition + direction * speed * delta;
		if (_game != null && _game.IsPositionInWater(nextPosition))
		{
			RandomizeWanderDirection();
			Velocity = Vector2.Zero;
			return;
		}

		Velocity = direction * speed;
		_lastDirection = direction;
		MoveAndCollide(Velocity * delta, false, 0.08f, true);
	}

	private void UpdateThreatened(float delta)
	{
		if (_targetFox == null)
		{
			_currentState = DeerState.Wandering;
			return;
		}

		if (IsHerdFemale && _herdMale != null)
		{
			float distanceToMale = GlobalPosition.DistanceTo(_herdMale.GlobalPosition);

			if (distanceToMale > HerdFreeRange)
			{
				Vector2 directionToMale = (_herdMale.GlobalPosition - GlobalPosition).Normalized();
				Velocity = directionToMale * WanderSpeed;
				_lastDirection = directionToMale;
				_lastAnimationPlayed = "";
				MoveAndCollide(Velocity * delta, false, 0.08f, true);
				return;
			}

			// Within safe distance, face the threat but stay ready to follow male
			Vector2 directionToFox = (_targetFox.GlobalPosition - GlobalPosition).Normalized();
			_lastDirection = directionToFox;
			_lastAnimationPlayed = "";
			Velocity = Vector2.Zero;
		}
		else
		{
			Vector2 directionToFox = (_targetFox.GlobalPosition - GlobalPosition).Normalized();
			_lastDirection = directionToFox;
			_lastAnimationPlayed = "";
			Velocity = Vector2.Zero;
		}

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

		if (IsHerdFemale && _herdMale != null)
		{
			float distanceToMale = GlobalPosition.DistanceTo(_herdMale.GlobalPosition);

			if (distanceToMale > HerdFreeRange)
			{
				Vector2 directionToMale = (_herdMale.GlobalPosition - GlobalPosition).Normalized();
				Velocity = directionToMale * ChasingSpeed;
				_lastDirection = directionToMale;
				_lastAnimationPlayed = "";
			}
			else
			{
				// Can chase the fox while staying near male
				Vector2 directionToFox = (_targetFox.GlobalPosition - GlobalPosition).Normalized();
				Velocity = directionToFox * ChasingSpeed;
				_lastDirection = directionToFox;
				_lastAnimationPlayed = "";
			}
		}
		else
		{
			// Chase the fox
			Vector2 directionToFox = (_targetFox.GlobalPosition - GlobalPosition).Normalized();
			Velocity = directionToFox * ChasingSpeed;
			_lastDirection = directionToFox;
			_lastAnimationPlayed = "";
		}

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
		if (Velocity.Length() < 1f)
		{
			ApplyGenderAnimation(_lastDirection, play: false);
			return;
		}

		ApplyGenderAnimation(Velocity, play: true);
	}

	private string ResolveAnimName(Vector2 direction, out string baseAnimName, out bool flipH)
	{
		float x = direction.X;
		float y = direction.Y;
		flipH = false;

		if (Mathf.Abs(x) > Mathf.Abs(y))
		{
			baseAnimName = "walk_right";
			if (x <= 0f)
				flipH = true;
		}
		else
			baseAnimName = y > 0f ? "walk_down" : "walk_up";

		string genderSuffix = _gender == Gender.Male ? "male" : "female";
		return $"{baseAnimName}_{genderSuffix}";
	}

	private void ApplyGenderAnimation(Vector2 direction, bool play)
	{
		if (direction.LengthSquared() < 0.0001f)
			direction = _lastDirection;

		string animName = ResolveAnimName(direction, out string baseAnimName, out bool flipH);
		string animToUse = animName;

		if (!_animSprite.SpriteFrames.HasAnimation(animName))
		{
			if (_animSprite.SpriteFrames.HasAnimation(baseAnimName))
				animToUse = baseAnimName;
			else
				return;
		}

		if (play)
		{
			if (_animSprite.Animation != animToUse || !_animSprite.IsPlaying())
				_animSprite.Play(animToUse);
		}
		else
		{
			_animSprite.Animation = animToUse;
			if (_animSprite.IsPlaying())
				_animSprite.Stop();
			_animSprite.SetFrameAndProgress(0, 0f);
		}

		_lastAnimationPlayed = animName;
		_animSprite.FlipH = baseAnimName == "walk_right" && flipH;
	}

	private void RandomizeWanderDirection()
	{
		float angle = (float)(GD.Randf() * Mathf.Tau);
		_wanderDirection = Vector2.FromAngle(angle);
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

	// Herd-related properties and methods
	public int HerdId => _herdId;
	public Gender DeerGender => _gender;
	public bool IsHerdMale => _gender == Gender.Male && _herdId >= 0;
	public bool IsHerdFemale => _gender == Gender.Female && _herdId >= 0;
	public int HerdFemaleCount => _herdFemales.Count;

	/// <summary>
	/// Initialize this deer as a herd male
	/// </summary>
	public void ConfigureHerdMale(int herdId)
	{
		_herdId = herdId;
		_gender = Gender.Male;
		_herdMale = null;
		_herdFemales.Clear();
	}

	public void ConfigureHerdFemale(int herdId, DeerMovement herdMale)
	{
		if (herdMale == null || herdMale.DeerGender != Gender.Male)
			return;

		_herdId = herdId;
		_gender = Gender.Female;
		_herdMale = herdMale;
	}

	public void InitializeAsHerdMale(int herdId) => ConfigureHerdMale(herdId);

	public void InitializeAsHerdFemale(int herdId, DeerMovement herdMale) => ConfigureHerdFemale(herdId, herdMale);

	/// <summary>
	/// Add a female to this male's herd
	/// </summary>
	public void AddFemaleToHerd(DeerMovement female)
	{
		if (!_herdFemales.Contains(female))
		{
			_herdFemales.Add(female);
		}
	}

	/// <summary>
	/// Remove a female from this male's herd
	/// </summary>
	public void RemoveFemaleFromHerd(DeerMovement female)
	{
		_herdFemales.Remove(female);
	}

	/// <summary>
	/// Get the herd male (for females to reference)
	/// </summary>
	public DeerMovement GetHerdMale() => _herdMale;

	public Vector2 GetWanderDirection() => _wanderDirection;

	public bool IsInHerd => _herdId >= 0;
}
