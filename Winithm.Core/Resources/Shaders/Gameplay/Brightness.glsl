#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Brightness; // %0.0%

varying vec2 v_texcoord;

void main() {
  vec4 color = texture2D(ScreenTexture, v_texcoord);
  color.rgb += Brightness;
  gl_FragColor = color;
}
