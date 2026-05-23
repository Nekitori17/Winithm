shader_type canvas_item;

uniform bool is_highlighted = false;
uniform vec4 glow_color : hint_color = vec4(1.0, 0.85, 0.2, 0.7);
uniform float glow_size : hint_range(0.0, 100.0) = 10.0;

void fragment() {
  if (is_highlighted) {
    vec2 ts = TEXTURE_PIXEL_SIZE * glow_size * 0.5;
    float a = 0.0;
    
    // Pseudo-Gaussian 13-tap blur
    a += texture(TEXTURE, UV).a * 0.20;
    
    a += texture(TEXTURE, UV + vec2(ts.x, 0.0)).a * 0.12;
    a += texture(TEXTURE, UV + vec2(-ts.x, 0.0)).a * 0.12;
    a += texture(TEXTURE, UV + vec2(0.0, ts.y)).a * 0.12;
    a += texture(TEXTURE, UV + vec2(0.0, -ts.y)).a * 0.12;
    
    a += texture(TEXTURE, UV + vec2(ts.x, ts.y)).a * 0.05;
    a += texture(TEXTURE, UV + vec2(-ts.x, ts.y)).a * 0.05;
    a += texture(TEXTURE, UV + vec2(ts.x, -ts.y)).a * 0.05;
    a += texture(TEXTURE, UV + vec2(-ts.x, -ts.y)).a * 0.05;
    
    a += texture(TEXTURE, UV + vec2(ts.x * 2.0, 0.0)).a * 0.03;
    a += texture(TEXTURE, UV + vec2(-ts.x * 2.0, 0.0)).a * 0.03;
    a += texture(TEXTURE, UV + vec2(0.0, ts.y * 2.0)).a * 0.03;
    a += texture(TEXTURE, UV + vec2(0.0, -ts.y * 2.0)).a * 0.03;
    
    // Bo góc mềm dần (Soft falloff)
    float glow_alpha = smoothstep(0.0, 0.6, a) * glow_color.a;

    COLOR = vec4(glow_color.rgb, glow_alpha);
  } else {
    COLOR = vec4(0.0);
  }
}