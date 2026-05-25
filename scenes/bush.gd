extends StaticBody2D

@onready var berry: PackedScene = preload("res://scenes/dropped_item.tscn")
@onready var good_bush: Sprite2D = $GoodBush
@onready var dry_bush: Sprite2D = $DryBush

@export var berry_drop: ItemData

static var rng = RandomNumberGenerator.new()
var is_alive = false

func _ready() -> void:
	# Setup interaction and default tree pose
	#make it a part of a group
	add_to_group("bush")
	dry_bush.show()

func set_alive(alive: bool):
	
	#so we only spawn berries when they are created 
	if is_alive == alive:
		return is_alive
		
	is_alive = alive

	if alive:
		dry_bush.hide()
		good_bush.show()
		for i in range(3):
			var dropped_item = berry.instantiate()
			dropped_item.ItemData = berry_drop
			dropped_item.global_position = global_position + Vector2(randi_range(-4, 4), randi_range(-4, 4))
			get_parent().add_child(dropped_item)
	else:
		good_bush.hide()
		dry_bush.show()
		
		
	return is_alive
	

	
