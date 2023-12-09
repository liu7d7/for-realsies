#version 460

layout(location = 0) out vec4 f_color;

layout(location = 0) in vec3 v_uv;

uniform samplerCube u_tex_skybox;

void main() {    
  f_color = texture(u_tex_skybox, v_uv);
}