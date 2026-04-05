#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Strength; // %0.5%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;
  vec2 center = vec2(0.5);
  vec2 delta = uv - center;
  float dist = length(delta);

  float power = 1.0 + Strength * 2.0;
  float bind = 0.5;

  vec2 warped = center + normalize(delta) * bind * pow(dist / bind, power);
  warped = mix(uv, warped, step(0.001, dist));

  gl_FragColor = texture2D(ScreenTexture, warped);
}
