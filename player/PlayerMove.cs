using Godot;
using System;
using System.Diagnostics;

public partial class PlayerMove : CharacterBody2D
{
    public const float Speed = 210.0f;
    private AnimatedSprite2D _animSprite;
    private AudioStreamPlayer2D _audioRunPlayer;
    private AudioStreamPlayer2D _audioSwimmingPlayer;
    private AudioStreamPlayer2D _jumpInWaterPlayer;

    public bool InWater = false;
    public double PlayNextWaterJumpSound = 0;

    // 保存最后朝向（用于idle）
    private Vector2 _lastDirection = Vector2.Down;


    public override void _Ready()
    {
        Debug.WriteLine("Camera Created");
        _animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _audioRunPlayer = GetNode<AudioStreamPlayer2D>("AudioRunPlayer");
        _audioSwimmingPlayer = GetNode<AudioStreamPlayer2D>("AudioSwimmingPlayer");
        _jumpInWaterPlayer = GetNode<AudioStreamPlayer2D>("JumpInWaterPlayer");

        //GetTree().Root.GetNode<Game>("Node2D").MainCamera = GetNode<Camera2D>("Camera2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = direction * (Speed * (float)(InWater ? 1.5 : 1.0));

        // 移动时更新最后方向
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
            var cardinal = (int)(4.0 * _lastDirection.Angle() / Mathf.Tau) + 1;
            var goingUp = cardinal == 0;
            var goingDown = cardinal == 2;

            var waterLineIfInWater = goingDown ? 0.15 : goingUp ? 0.3 : 0.4;
            var waterLine = InWater ? waterLineIfInWater : 0;

            asShaderMaterial.SetShaderParameter("water_line", waterLine);
        }

        if (InWater)
        {
            if (PlayNextWaterJumpSound <= 0)
            {
                

                if (!_jumpInWaterPlayer.Playing)
                { 
                    _jumpInWaterPlayer.Play();
                }
            }
            PlayNextWaterJumpSound = 1;
        }
        else
        {
            PlayNextWaterJumpSound = PlayNextWaterJumpSound - delta;
        }

        // 动画控制
        UpdateAnimation(direction);

        MoveAndSlide();
    }

    private void UpdateAnimation(Vector2 direction)
    {
        // 重置翻转
        _animSprite.FlipH = false;
        _animSprite.FlipV = false;

        bool isIdle = direction == Vector2.Zero;
        Vector2 currentDir = isIdle ? _lastDirection : direction;

        // 上下方向优先
        if (Mathf.Abs(currentDir.Y) > Mathf.Abs(currentDir.X))
        {
            if (currentDir.Y < 0)
            {
                // 上 ↑（独立动画，不翻转）
                _animSprite.Play(isIdle ? "idle_up" : "walk_up");
            }
            else
            {
                // 下 ↓（独立动画）
                _animSprite.Play(isIdle ? "idle_down" : "walk_down");
            }
        }
        else
        {
            if (currentDir.X < 0)
            {
                // 左 ←
                _animSprite.Play(isIdle ? "idle_left" : "walk_left");
            }
            else
            {
                // 右 → 用左动画 + 水平翻转
                _animSprite.Play(isIdle ? "idle_left" : "walk_left");
                _animSprite.FlipH = true;
            }
        }
    }
}