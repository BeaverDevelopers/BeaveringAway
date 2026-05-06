shader_type canvas_item;

uniform sampler2D noise_texture: repeat_enable, filter_nearest;
uniform vec3 green: source_color;
uniform vec3 greener: source_color;
uniform float density : hint_range(0.0, 100.0);

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	//COLOR = vec4(1,1,1, 1);
	float noise = texture(noise_texture, UV * density).r;
	COLOR = vec4(noise > 0.3 ? green : greener, 1.);
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
