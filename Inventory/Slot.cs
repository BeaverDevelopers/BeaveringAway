using Godot;
using System;
using System.ComponentModel;

public partial class Slot : TextureRect
{
	[Export] public ItemData currentItem; // get handle on current item

	public override void _Ready()
	{
		setItemSlot();
	}

	public void setItemSlot()
	{
		if (currentItem == null) //  if no item, return
		{
			GetNode<TextureRect>("ItemTexture").Texture = null;
			GetNode<Label>("ItemCountLabel").Text = "";
			return;
		}
		GetNode<TextureRect>("ItemTexture").Texture = currentItem.Icon; // set the slot to the item icon
		GetNode<Label>("ItemCountLabel").Text = currentItem.ItemCount.ToString(); // set the slot to the item count
	}


}
