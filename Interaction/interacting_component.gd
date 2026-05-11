extends Node2D

@onready var interact_text: Label = $"Interact Text"
const UI_EVENTS = preload("res://ui/Core/UIEventBus.cs")
#hold list of all the available interaction within the InteractRange
var current_interactions := [] 
var can_interact := true

func _input(event: InputEvent) -> void:
	#if e is pressed (defined in project settings Input Map)
	if event.is_action_pressed("interact") and can_interact:
		if current_interactions:
			can_interact = false
			interact_text.hide()
			UI_EVENTS.HideInteractionPrompt()
			
			await current_interactions[0].interact.call()
			
			can_interact = true



func _process(_delta: float) -> void:
	if current_interactions and can_interact:
		current_interactions.sort_custom(_sort_by_closest)
		if current_interactions[0].is_interactable:
			interact_text.text = current_interactions[0].interact_name
			interact_text.hide()
			UI_EVENTS.ShowInteractionPromptForNode(current_interactions[0].interact_name, self)
		else:
			interact_text.hide()
			UI_EVENTS.HideInteractionPrompt()
	else:
		interact_text.hide()
		UI_EVENTS.HideInteractionPrompt()

#sorts the array based on what is closest to the player
func _sort_by_closest(area1, area2):
		var area1_dist = global_position.distance_to(area1.global_position)
		var area2_dist = global_position.distance_to(area2.global_position)
		return area1_dist < area2_dist

#entering the interaction area
func _on_interact_range_area_entered(area: Area2D) -> void:
	current_interactions.push_back(area) #add this area to the end of the arrayy

#exiting the interaction area
func _on_interact_range_area_exited(area: Area2D) -> void:
	current_interactions.erase(area) #removes the area from the array
