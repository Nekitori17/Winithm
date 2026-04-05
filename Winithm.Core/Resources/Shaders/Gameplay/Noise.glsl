#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;
uniform float Time;

uniform float Amount; // %0.15%

varying vec2 v_texcoord;

float rand(vec2 co) {
  return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
  vec4 color = texture2D(ScreenTexture, v_texcoord);
  float grain = rand(v_texcoord * ScreenSize + vec2(Time * 100.0)) * 2.0 - 1.0;
  color.rgb += grain * Amount;
  gl_FragColor = color;
}
