#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Size; // %8.0%

varying vec2 v_texcoord;

void main() {
  vec2 pixels = ScreenSize / max(Size, 1.0);
  vec2 uv = floor(v_texcoord * pixels) / pixels;
  gl_FragColor = texture2D(ScreenTexture, uv);
}
