﻿#version 460

layout(location = 0) in vec2 pos;

layout(location = 0) out vec2 v_uv;

void main() {
  gl_Position = vec4(pos, 0., 1.);
  v_uv = pos * 0.5 + 0.5;
}