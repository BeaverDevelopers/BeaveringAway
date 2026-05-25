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
		#trying to prevent overlapping berries
		var y_coordinates = [randi_range(-10, 10), randi_range(-10, 10), randi_range(-10, 10), randi_range(-10, 10)]
		y_coordinates.sort()
		for i in range(1, y_coordinates.size()):
			if y_coordinates[i] - y_coordinates[i-1] < 5:
				if y_coordinates[i] <= 5:
					y_coordinates[i] = y_coordinates[i-1] + 5
				else:
					y_coordinates[i] += 3
					y_coordinates[i-1] -= 2
				
		
		for i in range(4):
			var dropped_item = berry.instantiate()
			dropped_item.ItemData = berry_drop
			#if i == 0:
				#dropped_item.global_position = good_bush.global_position + Vector2(-15, -10)
			#if i == 1:
				#dropped_item.global_position = good_bush.global_position + Vector2(-5, -10)
			#if i == 2:
				#dropped_item.global_position = good_bush.global_position + Vector2(-15, 10)
			var x = randi_range(-15, 13)
			dropped_item.global_position = good_bush.global_position + Vector2(x, y_coordinates[i])
			dropped_item.z_index = 1 #because otherwise they sometimes disappeared 
			get_parent().add_child(dropped_item)
	else:
		good_bush.hide()
		dry_bush.show()
		
		
	return is_alive
	

	
