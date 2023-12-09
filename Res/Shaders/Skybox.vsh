#version 460
layout(location = 0) in vec3 pos;

layout(location = 0) out vec3 v_uv;

uniform mat4 u_proj;
uniform mat4 u_view;

void main() {
  v_uv = pos;
  vec4 final = u_proj * u_view * vec4(pos, 1.0);
  gl_Position = final.xyww;
}