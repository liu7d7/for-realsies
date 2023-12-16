#version 460

layout(location = 0) in vec3 pos;
layout(location = 3) in mat4 model;

uniform mat4 u_proj;
uniform mat4 u_view;

void main() {
  gl_Position = u_proj * u_view * model * vec4(pos, 1.);
}