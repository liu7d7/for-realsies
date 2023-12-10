#version 460

layout(location = 0) in vec2 v_uv;
layout(location = 1) in vec3 v_tint;

layout(location = 0) out vec4 f_color;

uniform sampler2D u_tex;

void main() {
  f_color = vec4(v_tint, texture(u_tex, v_uv).r);
}