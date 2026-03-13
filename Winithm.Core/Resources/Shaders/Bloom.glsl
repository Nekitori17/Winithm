#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;

uniform float param_0; // %0.0%
uniform float threshold; // %0.8%
uniform float intensity; // %1.0%
uniform vec3 color; // %0.0, 0.0, 0.0%

varying vec2 v_texcoord;

void main() {
    vec4 tex_color = texture2D(ScreenTexture, v_texcoord);
    
    float luminance = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    float contribution = step(threshold, luminance);
    vec3 bright_areas = tex_color.rgb * contribution;
    vec3 final_bloom = bright_areas * bloom_color * intensity + vec3(param_0);
    
    gl_FragColor = tex_color + vec4(final_bloom, 0.0);
}
