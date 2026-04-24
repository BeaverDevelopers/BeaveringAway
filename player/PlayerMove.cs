using Godot;

public partial class PlayerMove : CharacterBody2D
{
    public const float Speed = 300.0f;
    private AnimatedSprite2D _animSprite;

    // 保存最后朝向（用于idle）
    private Vector2 _lastDirection = Vector2.Down;

    public override void _Ready()
    {
        _animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        GetTree().Root.GetNode<Game>("Node2D").MainCamera = GetNode<Camera2D>("Camera2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = direction * Speed;

        // 移动时更新最后方向
        if (direction != Vector2.Zero)
        {
            _lastDirection = direction;
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