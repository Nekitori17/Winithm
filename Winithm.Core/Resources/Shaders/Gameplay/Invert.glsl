#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Amount; // %1.0%

varying vec2 v_texcoord;

void main() {
  vec4 color = texture2D(ScreenTexture, v_texcoord);
  color.rgb = mix(color.rgb, 1.0 - color.rgb, Amount);
  gl_FragColor = color;
}
