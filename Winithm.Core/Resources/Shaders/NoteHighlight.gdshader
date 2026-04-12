shader_type canvas_item;

uniform bool is_highlighted = false;
uniform vec4 glow_color : hint_color = vec4(1.0, 0.85, 0.2, 0.7);
uniform float glow_size : hint_range(0.0, 15.0) = 5.0;

varying vec2 glow_uv;

void vertex() {
  float gs = glow_size * float(is_highlighted);
  vec2 tex_size = vec2(1.0) / TEXTURE_PIXEL_SIZE;
  vec2 expand_uv = vec2(gs) / tex_size;

  VERTEX += (UV * 2.0 - 1.0) * gs;

  glow_uv = mix(-expand_uv, vec2(1.0) + expand_uv, UV);
}

void fragment() {
  float inside = step(0.0, glow_uv.x) * step(glow_uv.x, 1.0)
                * step(0.0, glow_uv.y) * step(glow_uv.y, 1.0);

  vec4 tex_color = texture(TEXTURE, clamp(glow_uv, vec2(0.0), vec2(1.0))) * inside;

  vec2 d = max(max(-glow_uv, glow_uv - vec2(1.0)), vec2(0.0));
  vec2 norm = max(vec2(glow_size) * TEXTURE_PIXEL_SIZE, vec2(0.001));
  float dist = length(d / norm);

  float glow_alpha = smoothstep(1.0, 0.0, dist) * glow_color.a * float(is_highlighted);
  glow_alpha *= (1.0 - tex_color.a);

  COLOR = tex_color + vec4(glow_color.rgb * glow_alpha, glow_alpha);
}