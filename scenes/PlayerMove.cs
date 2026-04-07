using Godot;

public partial class PlayerMove : CharacterBody2D
{
    public const float Speed = 300.0f;

    public override void _PhysicsProcess(double delta)
    {
        // 获取方向
        Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

        // 直接计算速度（无重力、无跳跃、无下滑）
        Vector2 velocity = direction * Speed;

        // 赋值并移动
        Velocity = velocity;
        MoveAndSlide();
    }
}