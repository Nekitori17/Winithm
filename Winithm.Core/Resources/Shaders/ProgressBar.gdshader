shader_type canvas_item;
uniform vec4 color_left : hint_color;
uniform vec4 color_right : hint_color;
uniform float progress = 0.0;
uniform float gradient_width = 0.05;

void fragment() {
    float safe_progress = max(progress, 0.0001);
    float gradient_uv_width = gradient_width / safe_progress;
    float start_uv = 1.0 - gradient_uv_width;
    
    if (UV.x <= start_uv) {
        COLOR = color_left;
    } else {
        float t = (UV.x - start_uv) / gradient_uv_width;
        t = clamp(t, 0.0, 1.0);
        COLOR = mix(color_left, color_right, t);
    }
}
