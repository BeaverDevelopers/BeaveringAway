using Godot;

// 【唯一正确】Godot C# 支持内嵌编辑的类
public partial class ItemData : RefCounted
{
    // 面板直接显示的字段
    [Export] public int itemId;
    [Export] public Texture2D icon;
}