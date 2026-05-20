shader_type canvas_item;

uniform float water_line_bottom : hint_range(0,1) = 1;
uniform float water_line_top : hint_range(0,1) = 0;
uniform vec4 underwater_color: source_color = vec4(0, 0, 1, 1);

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec4 underwater_rgb = mix(COLOR, underwater_color, 1);
	underwater_rgb.a = COLOR.a;
	COLOR = (UV.y > water_line_top && UV.y < water_line_bottom) ? COLOR : underwater_rgb;
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
