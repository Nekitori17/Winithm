#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform vec2 Center;    // %0.5, 0.5%
uniform float Radius;   // %0.3%
uniform float Strength; // %0.5%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;
  float aspect = ScreenSize.x / ScreenSize.y;
  vec2 delta = (uv - Center) * vec2(aspect, 1.0);
  float dist = length(delta);

  float mask = smoothstep(Radius, 0.0, dist);
  vec2 dir = delta / (dist + 0.0001);

  // Pull pixels inward toward center (shrink)
  vec2 displaced = uv + dir * mask * Strength * dist;

  gl_FragColor = texture2D(ScreenTexture, displaced);
}
