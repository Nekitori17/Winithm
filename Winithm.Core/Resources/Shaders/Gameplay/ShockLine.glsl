#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;
uniform float Time;

uniform float Speed;     // %10.0%
uniform float Strength;  // %0.1%
uniform float Thickness; // %0.2%
uniform float WaveW;     // %30.0%
uniform float Frequency; // %5.0%
uniform float Degree;    // %0.0%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;

  float timeSeed = Time * Speed;

  // Calculate rotation trigonometry
  float rad = Degree * 3.14159265 / 180.0;
  float s = sin(rad);
  float c = cos(rad);

  // Rotate UV around center to find the local Axis (local Y = scrolling direction)
  vec2 centered = uv - vec2(0.5);
  float localY = -centered.x * s + centered.y * c + 0.5;

  // Calculate base scrolling position for shocklines based on rotated Y
  float wavePhase = localY * Frequency - timeSeed;
  float linePulse = sin(wavePhase) * 0.5 + 0.5; 
  
  // Thickness determines how wide the line is
  float lineMask = smoothstep(1.0 - Thickness * 0.4, 1.0, linePulse);

  // WaveW controls how jagged the interior is, based on rotated Y
  float waveOffset = sin(localY * WaveW + timeSeed * 2.0) * Strength * lineMask;
  
  // Displace along the local X axis (perpendicular to scrolling direction)
  vec2 dir = vec2(c, s);
  uv += dir * waveOffset;

  // Render texture and add flash glow based on mask
  vec4 color = texture2D(ScreenTexture, uv);
  color.rgb += lineMask * Strength * 0.8;

  gl_FragColor = color;
}
