using Godot;

public partial class ItemData : Resource
{
	// 用 [field: Export] 直接修饰自动属性
	[field: Export] public int itemId { get; set; }
	[field: Export] public Texture2D icon { get; set; }
}
