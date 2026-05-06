using Godot;

// 🔥 核心特性：必须添加！让自定义资源在编辑器中可用
[GlobalClass]
public partial class ItemData : Resource
{
    // 物品ID（唯一标识）
    [Export] public int ItemId;

    // 物品图标
    [Export] public Texture2D Icon;

    [Export] public int ItemCount = 1;
}