#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Strength; // %0.01%
uniform float Angle;    // %0.0%
uniform float Samples;  // %8.0%

varying vec2 v_texcoord;

void main() {
  float rad = Angle * 3.14159265 / 180.0;
  vec2 dir = vec2(cos(rad), sin(rad)) * Strength;
  float sampleCount = max(floor(Samples), 1.0);

  vec4 sum = vec4(0.0);
  for (float i = 0.0; i < 32.0; i += 1.0) {
    if (i >= sampleCount) break;
    float t = (i / (sampleCount - 1.0)) - 0.5;
    sum += texture2D(ScreenTexture, v_texcoord + dir * t);
  }

  gl_FragColor = sum / sampleCount;
}
