#version 460

layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 uv;
layout(location = 2) in vec3 norm;

layout(location = 0) out vec3 v_pos;
layout(location = 1) out vec2 v_uv;
layout(location = 2) out vec3 v_norm;
layout(location = 3) out vec4 v_light_space_pos;

uniform mat4 u_proj;
uniform mat4 u_view;
uniform mat4 u_light_proj;
uniform mat4 u_light_view;
uniform mat4 u_model = mat4(1.);

void main() {
  gl_Position = u_proj * u_view * u_model * vec4(pos, 1.);
  v_light_space_pos = u_light_proj * u_light_view * u_model * vec4(pos, 1.);
  v_pos = pos;
  v_uv = uv;
  v_norm = mat3(transpose(inverse(u_model))) * norm;
}