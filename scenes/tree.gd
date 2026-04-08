extends StaticBody2D

@onready var interactable: Area2D = $Interactable
@onready var sprite_2d: Sprite2D = $Sprite2D
#@onready var log: Sprite2D = $Log
#@onready var log_2: Sprite2D = $Log2
#@onready var log_3: Sprite2D = $Log3
@onready var LOG: PackedScene = preload("res://scenes/log.tscn") #loads the log scene
@onready var collision_shape_2d: CollisionShape2D = $CollisionShape2D


func _ready() -> void:
	interactable.interact = _on_interact
	
	
func _on_interact():
	if sprite_2d.frame == 0:
		sprite_2d.frame = 1
		#log.visible = true
		remove_child(collision_shape_2d) #removes the collision
		remove_child(interactable) #removes the interactable component that should now be the logs
		interactable.is_interactable = false
		print("The beaver chopped a tree")
		#spawns 3 logs for the player to pick up
		for i in range(3):
			var log = LOG.instantiate() 
			log.position = Vector2(randi_range(-40,40), randi_range(-40,40))
			add_child(log) #add the scene logs to the tree
