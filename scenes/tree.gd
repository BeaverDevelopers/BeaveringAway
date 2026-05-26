extends StaticBody2D

@onready var interactable: Interactable = $Interactable
@onready var tree_anim: AnimatedSprite2D = $TreeAnim
@onready var LOG: PackedScene = preload("res://scenes/dropped_item.tscn")
@onready var collision_shape_2d: CollisionShape2D = $CollisionShape2D
@onready var tree_falling_audio: AudioStreamPlayer2D = $FallingSoundPlayer
@onready var tree_chomp_audio: AudioStreamPlayer2D = $ChompSoundPlayer
@onready var dry_tree: Sprite2D = $DryTree
@onready var leaf_tree_sprite: Sprite2D = $LeafTree #if we add animation then we should just export tree animation and set it in inspector
@onready var dry_tree_audio: AudioStreamPlayer2D = $DryTreeSoundPlayer

@export var item_drop: ItemData
@export var sappling_drop: ItemData 
@export var leaf_drop: ItemData
@export var leaf_tree = false

static var rng = RandomNumberGenerator.new()
static var max_health = 6
var health = max_health
var is_alive = false

func _ready() -> void:
	# Setup interaction and default tree pose
	#make it a part of a group
	add_to_group("trees")
	interactable.interact = _on_interact
	dry_tree.show()
	tree_anim.play("fall_right")
	tree_anim.pause() # Stay on first frame (standing tree)
	
	


	var newMaterial = tree_anim.material.duplicate()
	tree_anim.material = newMaterial
	if newMaterial is ShaderMaterial:
		newMaterial.set_shader_parameter("wind_offset", rng.randf_range(-10.0, 10.0))

func set_alive(alive: bool):
	is_alive = alive

	if alive:
		dry_tree.hide()
		if not leaf_tree:
			tree_anim.show()
		else:
			leaf_tree_sprite.show()
	else:
		dry_tree.show()
		if leaf_tree:
			leaf_tree_sprite.hide()
		else:
			tree_anim.hide()
		
	return is_alive
	

func _on_interact():
	# Prevent duplicate interactions
	if not is_inside_tree():
		return
		
	tree_chomp_audio.play()
	

	# Check if we should just chomp the tree instead.
	if health > 1:
		interactable.interact_name = "Chomp Tree (%d/%d)" % [health - 1, max_health]
		await get_tree().create_timer(0.5).timeout
		
		health -= 1
		return

	# Get player from group
	var player = null
	for node in get_tree().get_nodes_in_group("player"):
		player = node
		break

	if not player:
		print("Player not found! Add player to group: player")
		return

	# Check which side the player is on
	var player_side = player.global_position.x - global_position.x
	
	#if the tree is "alive"
	if is_alive:
		tree_falling_audio.play()
		if not leaf_tree:
			await get_tree().create_timer(1).timeout
			
			# If we are on the left hand side, we want it to fall.
			if player_side > 0:
				tree_anim.play("fall_right") 
			else:
				tree_anim.play("fall_left")
				tree_anim.position = Vector2(-42, 4)

		


			# Wait for fall animation to finish
			await tree_anim.animation_finished
		
	else:
		dry_tree_audio.play()
		print("You chopped down a dead tree, nothing here for you")
		await get_tree().create_timer(1.5).timeout
		

	# Remove collision and interaction after falling
	remove_child(collision_shape_2d)
	remove_child(interactable)
	print("Tree chopped down successfully!")
	
	if is_alive:
		if leaf_tree:
			for i in range(2):
				var dropped_item = LOG.instantiate()
				dropped_item.ItemData = item_drop
				dropped_item.global_position = global_position + Vector2(randi_range(-40, 40), randi_range(-40, 40))
				get_parent().add_child(dropped_item)
			for i in range (2):
				var leaf = LOG.instantiate()
				leaf.ItemData = leaf_drop
				leaf.global_position = global_position + Vector2(randi_range(-40, 40), randi_range(-40, 40))
				get_parent().add_child(leaf)
		else:
		# Spawn 3 logs into the world (not as tree children)
			for i in range(3):
				var dropped_item = LOG.instantiate()
				dropped_item.ItemData = item_drop
				dropped_item.global_position = global_position + Vector2(randi_range(-40, 40), randi_range(-40, 40))
				get_parent().add_child(dropped_item)
		
		#if you are lucky spawn a sappling
		var random_sappling = randi_range(1, 10)
		if random_sappling < 5:
			print("You got lucky and got a sappling!")
			var sappling = LOG.instantiate()
			sappling.ItemData = sappling_drop
			sappling.global_position = global_position + Vector2(randi_range(-40, 40), randi_range(-40, 40))
			get_parent().add_child(sappling)
			

	# Delete the tree after everything is spawned
	queue_free()
