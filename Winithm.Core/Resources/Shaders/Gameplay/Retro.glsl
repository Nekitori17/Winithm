#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;
uniform float Time;

uniform float ScanlineIntensity; // %0.3%
uniform float Curvature;         // %0.02%

varying vec2 v_texcoord;

void main() {
  vec2 uv = v_texcoord;

  // CRT barrel curvature
  vec2 centered = uv * 2.0 - 1.0;
  centered *= 1.0 + Curvature * dot(centered, centered);
  uv = centered * 0.5 + 0.5;

  // Clamp out-of-bounds to black
  float mask = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);

  vec4 color = texture2D(ScreenTexture, uv) * mask;

  // Scanlines
  float scanline = sin((uv.y * ScreenSize.y + Time * 60.0) * 3.14159) * 0.5 + 0.5;
  color.rgb *= 1.0 - ScanlineIntensity * (1.0 - scanline);

  gl_FragColor = color;
}
