#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Splits; // %2.0%
uniform float Axis;   // %0.0%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;
  float s = max(Splits, 1.0);

  // Axis: 0.0 = X split, 1.0 = Y split, anything between = blend
  float xCoord = mix(fract(uv.x * s), uv.x, step(0.5, Axis));
  float yCoord = mix(uv.y, fract(uv.y * s), step(0.5, Axis));

  gl_FragColor = texture2D(ScreenTexture, vec2(xCoord, yCoord));
}
