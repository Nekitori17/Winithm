#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;
uniform float Time;

uniform float Intensity;   // %0.2%
uniform float JitterSpeed; // %2.0%

varying vec2 v_texcoord;

float rand(vec2 co) {
  return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
  vec2 uv = v_texcoord;

  // Tracking line jitter
  float lineY = fract(Time * JitterSpeed * 0.1);
  float lineDist = abs(uv.y - lineY);
  float lineEffect = smoothstep(0.02, 0.0, lineDist) * Intensity;
  uv.x += lineEffect * (rand(vec2(uv.y, floor(Time * 30.0))) - 0.5);

  // Tape wobble
  float wobble = sin(uv.y * 50.0 + Time * JitterSpeed * 10.0) * Intensity * 0.003;
  uv.x += wobble;

  // Chromatic aberration split
  float aberration = Intensity * 0.01;
  float r = texture2D(ScreenTexture, uv + vec2(aberration, 0.0)).r;
  float g = texture2D(ScreenTexture, uv).g;
  float b = texture2D(ScreenTexture, uv - vec2(aberration, 0.0)).b;

  vec3 color = vec3(r, g, b);

  // Noise grain
  float noise = rand(uv * ScreenSize + vec2(Time * 77.0)) * Intensity * 0.3;
  color += noise;

  gl_FragColor = vec4(color, texture2D(ScreenTexture, v_texcoord).a);
}
