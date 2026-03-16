#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float Flash;      // %0.0%
uniform float Threshold;  // %0.8%
uniform float Intensity;  // %1.0%
uniform vec4 Color;       // %1.0, 1.0, 1.0, 1.0%

varying vec2 v_texcoord;

void main() {
  vec4 tex_color = texture2D(ScreenTexture, v_texcoord);
  
  float luminance = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
  
  float contribution = step(Threshold, luminance);
  vec3 bright_areas = tex_color.rgb * contribution;
  
  vec3 final_bloom = bright_areas * Color.rgb * Intensity;
  vec3 flash_effect = vec3(Flash);
  
  gl_FragColor = vec4(tex_color.rgb + final_bloom + flash_effect, tex_color.a);
}
