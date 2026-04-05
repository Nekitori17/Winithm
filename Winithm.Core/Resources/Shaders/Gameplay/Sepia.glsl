#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Amount; // %1.0%

varying vec2 v_texcoord;

void main() {
  vec4 color = texture2D(ScreenTexture, v_texcoord);
  float luma = dot(color.rgb, vec3(0.299, 0.587, 0.114));
  vec3 sepia = vec3(luma * 1.2, luma * 1.0, luma * 0.8);
  color.rgb = mix(color.rgb, sepia, Amount);
  gl_FragColor = color;
}
