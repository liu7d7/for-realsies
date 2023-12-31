﻿#version 460

layout(location = 0) in float layer;
layout(location = 1) in float slice;

layout(location = 0) out vec3 v_pos;
layout(location = 1) out vec2 v_uv;
layout(location = 2) out vec3 v_norm;

uniform mat4 u_proj;
uniform mat4 u_view;
uniform mat4 u_model;
uniform float u_start;
uniform float u_end;
uniform float u_slices;
uniform float u_layers;
uniform float u_time;

vec3 get_pos(float l, float s) {
  float arc = radians(mix(u_start, u_end, s / u_slices));
  vec3 pos = vec3(cos(arc), (l - 1) / u_layers, sin(arc));
  
  const float hs = 0.75, vs = 1.25;
  vec3 final_pos = pos * vec3(hs, vs, hs);
  if (pos.y > 0.7) return final_pos * vec3(0, 1, 0);
  final_pos.xz *= pow(abs(1 - pos.y - 0.3) * 2, .5) * 1;
  final_pos.y += pow(abs(1 - pos.y - 0.3) * 2, .5) * 0.08 * 
    (sin(arc * 3 + u_time * 1.33) + cos(arc * 5 + u_time * 0.66));
  return final_pos;
}

vec3 get_normal() {
  // ****0***1
  // *  **  **
  // * * * * *
  // **  **  *
  // 0***m***2
  // *  **  **
  // * * * * *
  // 3*  4*  *
  
  vec3 a = get_pos(layer, slice);
  vec3 b = get_pos(layer - 1, slice);
  vec3 c = get_pos(layer, slice - 1);
  
//   vec3 b[] = {
//     get_pos(layer - 1, slice), 
//     get_pos(layer - 1, slice), 
//     get_pos(layer - 1, slice + 1), 
//     get_pos(layer, slice - 1), 
//     get_pos(layer + 1, slice - 1), 
//     get_pos(layer + 1, slice)
//   };
  
//   vec3 c[] = {
//     get_pos(layer, slice - 1),
//     get_pos(layer - 1, slice + 1),
//     get_pos(layer, slice + 1),
//     get_pos(layer + 1, slice - 1),
//     get_pos(layer + 1, slice),
//     get_pos(layer, slice + 1)
//   };
 
//   vec3 accum = vec3(0.);
//   for (int i = 0; i < 6; i++) {
//     accum += cross(b[i] - a, c[i] - a); 
//   }
  
  return normalize(cross(b - a, c - a));
}

void main() {
  vec3 pos = get_pos(layer, slice);
  gl_Position = u_proj * u_view * u_model * vec4(pos, 1.);
  v_pos = pos;
  v_norm = mat3(transpose(inverse(u_model))) * get_normal();
}