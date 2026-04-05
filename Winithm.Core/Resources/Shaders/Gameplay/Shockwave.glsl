#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform vec2 Center;     // %0.5, 0.5%
uniform float Thickness; // %0.05%
uniform float Force;     // %0.04%
uniform float Progress;  // %0.0%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;
  float aspect = ScreenSize.x / ScreenSize.y;
  vec2 scaledUV = (uv - Center) * vec2(aspect, 1.0);
  float dist = length(scaledUV);

  float ring = abs(dist - Progress);
  float mask = smoothstep(Thickness, 0.0, ring);

  vec2 dir = normalize(scaledUV + vec2(0.0001));
  vec2 displaced = uv + dir * mask * Force;

  gl_FragColor = texture2D(ScreenTexture, displaced);
}
