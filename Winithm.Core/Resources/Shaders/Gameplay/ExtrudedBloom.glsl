#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float LayerCount;    // %10.0%
uniform float Spread;        // %0.018%
uniform float BloomRadius;   // %0.012%
uniform float LumaThreshold; // %0.12%
uniform float PerspStrength; // %0.3%

varying vec2 v_texcoord;

const vec3 LUMA = vec3(0.299, 0.587, 0.114);

void main() {
  vec2 uv = v_texcoord;
  vec2 norm = normalize(vec2(0.5) - uv);
  float aspect = ScreenSize.x / ScreenSize.y;

  vec4 base = texture2D(ScreenTexture, uv);
  vec3 col = base.rgb;

  float layers = max(1.0, floor(LayerCount));
  float lumaLo = LumaThreshold - 0.04;
  float lumaHi = LumaThreshold + 0.10;

  for (int i = 1; i < 24; i++) {
    float fi = float(i);
    float active = step(fi, layers);
    float t = fi / layers;
    vec2 sampleUV = uv + norm * Spread * t * (1.0 + PerspStrength * t * 0.4);
    vec3 s = texture2D(ScreenTexture, sampleUV).rgb;
    float objectness = smoothstep(lumaLo, lumaHi, dot(s, LUMA));
    col += s * ((1.0 - t) * 0.7 * objectness * active);
  }

  vec3 bloom = vec3(0.0);
  vec2 br = vec2(BloomRadius / aspect, BloomRadius);
  bloom += texture2D(ScreenTexture, uv + br * vec2( 1.0,  0.0)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2(-1.0,  0.0)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2( 0.0,  1.0)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2( 0.0, -1.0)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2( 0.707,  0.707)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2(-0.707,  0.707)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2( 0.707, -0.707)).rgb;
  bloom += texture2D(ScreenTexture, uv + br * vec2(-0.707, -0.707)).rgb;
  bloom *= 0.125;

  float baseLuma = dot(base.rgb, LUMA);
  float bloomLuma = dot(bloom, LUMA);
  float bw = max(smoothstep(0.25, 0.75, baseLuma), smoothstep(LumaThreshold, 0.5, bloomLuma)) * 0.45;
  col += bloom * bw;

  gl_FragColor = vec4(col, base.a);
}
