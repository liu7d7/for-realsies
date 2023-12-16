#version 460

layout(location = 0) in vec3 pos;

uniform mat4 u_proj;
uniform mat4 u_view;
uniform mat4 u_model = mat4(1.);

void main() {
  gl_Position = u_proj * u_view * u_model * vec4(pos, 1.);
}