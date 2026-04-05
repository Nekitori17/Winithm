#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Amount; // %1.0%

varying vec2 v_texcoord;

void main() {
  vec4 color = texture2D(ScreenTexture, v_texcoord);
  float luma = dot(color.rgb, vec3(0.299, 0.587, 0.114));
  color.rgb = mix(color.rgb, vec3(luma), Amount);
  gl_FragColor = color;
}
