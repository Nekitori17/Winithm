#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform vec2 Center;    // %0.5, 0.5%
uniform float Radius;   // %0.3%
uniform float Feather;  // %0.1%
uniform float Darken;   // %0.8%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;
  float aspect = ScreenSize.x / ScreenSize.y;
  vec2 delta = (uv - Center) * vec2(aspect, 1.0);
  float dist = length(delta);

  float circle = smoothstep(Radius, Radius + Feather, dist);

  vec4 color = texture2D(ScreenTexture, uv);
  color.rgb *= 1.0 - circle * Darken;

  gl_FragColor = color;
}
