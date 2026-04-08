extends StaticBody2D
@onready var interactable: Area2D = $Log/Interactable
@onready var log: Sprite2D = $Log
const InventoryData = preload("res://player/InventoryData.cs")


func _ready() -> void:
	interactable.interact = _on_interact
	
func _on_interact():
	remove_child(log)
	interactable.is_interactable = false
	InventoryData.AddItem(1,1)
	print("The beaver picked up a log")
