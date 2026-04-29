extends StaticBody2D

@onready var interactable: Area2D = $Interactable
@onready var sprite_2d: Sprite2D = $Sprite2D
@onready var FOX: PackedScene = preload("res://fox/fox.tscn") #loads the fox scene
@onready var LOG: PackedScene = preload("res://scenes/log.tscn") #loads the log scene
@onready var collision_shape_2d: CollisionShape2D = $CollisionShape2D


func _ready() -> void:
	interactable.interact = _on_interact
	
	
func _on_interact():
	
	#spawns the fox
	var fox = FOX.instantiate()
	fox.position = Vector2(60,60) #sets position of the fox
	add_sibling(fox) #adds the fox in the scene
	
	#change the tree to a stump
	if sprite_2d.frame == 0:
		sprite_2d.frame = 1
		remove_child(collision_shape_2d) #removes the collision
		remove_child(interactable) #removes the interactable component that should now be the logs
		interactable.is_interactable = false
		print("The beaver chopped a tree")
		#spawns 3 logs for the player to pick up
		for i in range(3):
			var log = LOG.instantiate() 
			log.position = Vector2(randi_range(-40,40), randi_range(-40,40))
			add_child(log) #add the scene logs to the tree
