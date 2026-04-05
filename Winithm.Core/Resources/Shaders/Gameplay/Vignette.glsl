#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Smoothness; // %0.4%
uniform float Radius;     // %0.8%
uniform vec4 Color;       // %0.0, 0.0, 0.0, 1.0%

varying vec2 v_texcoord;

void main() {
  vec4 tex = texture2D(ScreenTexture, v_texcoord);
  vec2 uv = v_texcoord;
  float dist = length(uv - vec2(0.5));

  float vig = smoothstep(Radius, Radius - Smoothness, dist);
  vec3 result = mix(Color.rgb, tex.rgb, vig);

  gl_FragColor = vec4(result, tex.a);
}
