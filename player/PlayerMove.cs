using Godot;
using System;
using System.Diagnostics;

public partial class PlayerMove : CharacterBody2D
{
    public const float Speed = 210.0f;
    public Vector2 MoveTarget = Vector2.Zero;
    public bool InWater = false;
    public bool DigOnArrival = false;
    public ItemData ItemToPlace = null;

    private double _playNextWaterJumpSound = 0;
    private Vector2 _lastDirection = Vector2.Down;

    private AnimatedSprite2D _animSprite;
    private AudioStreamPlayer2D _audioRunPlayer;
    private AudioStreamPlayer2D _audioSwimmingPlayer;
    private AudioStreamPlayer2D _jumpInWaterPlayer;
    private AudioStreamPlayer2D _foxStealPlayer;
    private AudioStreamPlayer2D _audioDigPlayer;

    public override void _Ready()
    {
        Debug.WriteLine("Camera Created");
        _animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _audioRunPlayer = GetNode<AudioStreamPlayer2D>("AudioRunPlayer");
        _audioSwimmingPlayer = GetNode<AudioStreamPlayer2D>("AudioSwimmingPlayer");
        _jumpInWaterPlayer = GetNode<AudioStreamPlayer2D>("JumpInWaterPlayer");
        _audioDigPlayer = GetNode<AudioStreamPlayer2D>("AudioDigPlayer");

        _foxStealPlayer = new AudioStreamPlayer2D { Name = "FoxStealPlayer" };
        _foxStealPlayer.Stream = GD.Load<AudioStream>("res://sounds/stealSound.wav");
        AddChild(_foxStealPlayer);
    }

    public void PlayDigSound()
    {
        _audioDigPlayer.Play();
    }

    public void PlayFoxStealFeedback(Texture2D itemIcon)
    {
        if (_foxStealPlayer?.Stream != null)
            _foxStealPlayer.Play();

        if (itemIcon == null)
            return;

        var floater = new StolenItemFloater();
        AddChild(floater);
        floater.Play(itemIcon);
    }

    public override void _PhysicsProcess(double delta)
    {

        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        if (direction != Vector2.Zero)
        {
            MoveTarget = Vector2.Zero;
            ItemToPlace = null;
            DigOnArrival = false;
        }
        else if (MoveTarget != Vector2.Zero)
        {
            Vector2 moveDelta = MoveTarget - Position;
            direction = moveDelta.Normalized();
            if (moveDelta.Length() < 10)
            {
                MoveTarget = Vector2.Zero;
                // Don't reset ItemToPlace here, the game will take care of that.
            }
        }

        if (direction != Vector2.Zero)
        {
            _lastDirection = direction;
            if (InWater)
            {
                if (!_audioSwimmingPlayer.Playing)
                {
                    _audioSwimmingPlayer.Play();
                }
                _audioRunPlayer.Stop();
            }
            else
            {
                if (!_audioRunPlayer.Playing)
                {
                    _audioRunPlayer.Play();
                }
            }
        }
        else {
            _audioRunPlayer.Stop();
            _audioSwimmingPlayer.Stop();
        }

        if (_animSprite.Material is ShaderMaterial asShaderMaterial)
        {
            var cardinal = Mathf.PosMod(Mathf.Round(_lastDirection.Angle() / (Mathf.Tau / 4)) + 1, 4);
            var goingUp = cardinal == 0;
            var goingDown = cardinal == 2;

            var waterLineIfInWater = goingDown ? 0.03 : goingUp ? 0.3 : 0.4;
            var waterLine = InWater ? waterLineIfInWater : 0;

            var waterLineTop = InWater ? (goingDown ? 0.65 : 0) : 0;

            asShaderMaterial.SetShaderParameter("water_line_bottom", 1 - waterLine);
            asShaderMaterial.SetShaderParameter("water_line_top", waterLineTop);
        }

        if (InWater)
        {
            if (_playNextWaterJumpSound <= 0)
            {
                

                if (!_jumpInWaterPlayer.Playing)
                { 
                    _jumpInWaterPlayer.Play();
                }
            }
            _playNextWaterJumpSound = 1;
        }
        else
        {
            _playNextWaterJumpSound = _playNextWaterJumpSound - delta;
        }

        UpdateAnimation(direction);

        Velocity = direction * (Speed * (float)(InWater ? 1.5 : 1.0));
        MoveAndSlide();
    }

    public void MoveAndPlaceItem(Vector2 where, ItemData item)
    {
        MoveTarget = where;
        ItemToPlace = item;
    }

    private void UpdateAnimation(Vector2 direction)
    {
        _animSprite.FlipH = false;
        _animSprite.FlipV = false;

        bool isIdle = direction == Vector2.Zero;
        Vector2 currentDir = isIdle ? _lastDirection : direction;

        var cardinal = Mathf.PosMod(Mathf.Round(_lastDirection.Angle() / (Mathf.Tau / 4)) + 1, 4);
        if (cardinal == 0)
        {
            _animSprite.Play(isIdle ? "idle_up" : "walk_up");
        }
        else if (cardinal == 1)
        {
            _animSprite.Play(isIdle ? "idle_left" : "walk_left");
            _animSprite.FlipH = true;
        }
        else if (cardinal == 2)
        {
            _animSprite.Play(isIdle ? "idle_down" : "walk_down");
        }
        else if (cardinal == 3)
        {
            _animSprite.Play(isIdle ? "idle_left" : "walk_left");
        }
    }
}