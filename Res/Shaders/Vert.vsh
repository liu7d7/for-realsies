#version 460

layout(location = 0) in vec3 pos;\

uniform mat4 u_proj;
uniform mat4 u_view;

void main() {
  gl_Position = u_proj * vec4(pos, 1.);
}