extends StaticBody2D

@onready var interactable: Area2D = $Interactable
@onready var tree_anim: AnimatedSprite2D = $TreeAnim
@onready var FOX: PackedScene = preload("res://fox/fox.tscn")
@onready var LOG: PackedScene = preload("res://scenes/log.tscn")
@onready var collision_shape_2d: CollisionShape2D = $CollisionShape2D
@onready var tree_falling_audio: AudioStreamPlayer2D = $FallingSoundPlayer

func _ready() -> void:
	# Setup interaction and default tree pose
	interactable.interact = _on_interact
	tree_anim.play("fall_right")
	tree_anim.pause() # Stay on first frame (standing tree)

func _on_interact():
	# Prevent duplicate interactions
	if not is_inside_tree():
		return

	# Get player from group
	var player = null
	for node in get_tree().get_nodes_in_group("player"):
		player = node
		break

	if not player:
		print("Player not found! Add player to group: player")
		return

	# Spawn fox at tree position offset
	var fox = FOX.instantiate()
	fox.global_position = global_position + Vector2(60, 60)
	get_parent().add_child(fox) # Spawn to world, not tree

	# Check which side the player is on
	var player_side = player.global_position.x - global_position.x
	
	tree_falling_audio.play()
	await get_tree().create_timer(1).timeout
	
	# Play fall animation based on player position
	if player_side > 0:
		tree_anim.play("fall_right") # Player on right → fall left
	else:
		tree_anim.play("fall_right") # Player on left → fall right

	# Wait for fall animation to finish
	await tree_anim.animation_finished

	# Remove collision and interaction after falling
	remove_child(collision_shape_2d)
	remove_child(interactable)
	print("Tree chopped down successfully!")

	# Spawn 3 logs into the world (not as tree children)
	for i in range(3):
		var log = LOG.instantiate()
		log.global_position = global_position + Vector2(randi_range(-40, 40), randi_range(-40, 40))
		get_parent().add_child(log)

	# Delete the tree after everything is spawned
	queue_free()
