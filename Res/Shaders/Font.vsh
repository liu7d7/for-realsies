﻿#version 460

layout(location = 0) in vec2 pos;
layout(location = 1) in vec2 uv;
layout(location = 2) in vec3 tint;

layout(location = 0) out vec2 v_uv;
layout(location = 1) out vec3 v_tint;

uniform mat4 u_proj;

void main() {
  gl_Position = u_proj * vec4(pos, 0, 1);
  v_uv = uv;
  v_tint = tint;
}