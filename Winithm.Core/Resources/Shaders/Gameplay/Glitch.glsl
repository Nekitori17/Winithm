#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;
uniform float Time;

uniform float Power;      // %0.1%
uniform float Rate;       // %0.2%
uniform float Speed;      // %15.0%
uniform float BlockCount; // %30.0%
uniform float ColorRate;  // %0.02%

varying vec2 v_texcoord;

float rand(vec2 co) {
  return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
  vec2 uv = v_texcoord;

  float blockY = floor(uv.y * BlockCount);
  float timeSeed = floor(Time * Speed);
  float noise = rand(vec2(blockY, timeSeed));
  float glitchStrength = step(1.0 - Rate, noise);

  float offset = (rand(vec2(blockY, timeSeed + 10.0)) - 0.5) * 2.0 * Power * glitchStrength;

  vec2 displaced = vec2(uv.x + offset, uv.y);

  float chromaticOff = ColorRate * glitchStrength;
  float r = texture2D(ScreenTexture, displaced + vec2(chromaticOff, 0.0)).r;
  float g = texture2D(ScreenTexture, displaced).g;
  float b = texture2D(ScreenTexture, displaced - vec2(chromaticOff, 0.0)).b;

  gl_FragColor = vec4(r, g, b, texture2D(ScreenTexture, uv).a);
}
