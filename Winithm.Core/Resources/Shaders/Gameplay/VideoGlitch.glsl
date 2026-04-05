#version 100
precision mediump float;

uniform sampler2D ScreenTexture;
uniform vec2 ScreenSize;
uniform float Time;

uniform float Intensity;  // %0.2%
uniform float BlockScale; // %20.0%
uniform float Speed;      // %20.0%
uniform float Rate;       // %0.3%

varying vec2 v_texcoord;

float rand(vec2 co) {
  return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
  vec2 uv = v_texcoord;
  
  float timeStep = floor(Time * Speed); 

  vec2 blockUv = floor(uv * BlockScale) / BlockScale;
  
  float corruptProb = rand(blockUv + timeStep);
  float isCorrupt = step(1.0 - Rate, corruptProb);
  
  vec2 motionVector = vec2(
    rand(blockUv + timeStep * 1.5) - 0.5,
    rand(blockUv + timeStep * 2.5) - 0.5
  ) * Intensity * 0.2;
  
  vec2 displacedUV = clamp(uv + motionVector * isCorrupt, 0.0, 1.0);
  
  vec4 color = texture2D(ScreenTexture, displacedUV);
  
  float quantizeSteps = clamp(20.0 - Intensity * 15.0, 2.0, 20.0);
  vec3 quantColor = floor(color.rgb * quantizeSteps) / quantizeSteps;
  
  float extremeCorrupt = step(1.0 - (Intensity * 0.25), corruptProb);
  float r = texture2D(ScreenTexture, clamp(displacedUV + motionVector * 3.0, 0.0, 1.0)).r;
  float b = texture2D(ScreenTexture, clamp(displacedUV - motionVector * 3.0, 0.0, 1.0)).b;
  
  vec3 finalColor = mix(color.rgb, quantColor, isCorrupt * 0.5);
  finalColor.r = mix(finalColor.r, r, extremeCorrupt);
  finalColor.b = mix(finalColor.b, b, extremeCorrupt);
  
  gl_FragColor = vec4(finalColor, color.a);
}
