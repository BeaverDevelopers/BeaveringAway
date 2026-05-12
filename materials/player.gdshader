shader_type canvas_item;

uniform float water_line : hint_range(0,1) = 0;


void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	COLOR.a = (1.0 - UV.y) > water_line ? COLOR.a : 0.0;
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
