#version 460

layout(location = 0) in vec3 v_pos;
layout(location = 1) in vec2 v_uv;
layout(location = 2) in vec3 v_norm;

layout(location = 0) out vec4 f_color;

uniform vec3 u_eye;
uniform vec3 u_ambi;
uniform vec3 u_diff;
uniform vec3 u_spec;
uniform bool u_has_norm_tex;
uniform sampler2D u_norm_tex;

const vec3 light_dir = vec3(-1, 2, -1);

void main() {
  vec3 norm = u_has_norm_tex ? normalize(2 * (texture(u_norm_tex, v_uv).rgb - 0.5)) : normalize(v_norm);
  
  float diffuse_strength = max(dot(norm, light_dir), 0.);
  vec3 diffuse = diffuse_strength * u_diff;
  
  vec3 view_dir = normalize(u_eye - v_pos);
  vec3 reflect_dir = normalize(reflect(-light_dir, norm));
  float specular_strength = pow(max(dot(view_dir, reflect_dir), 0.), 32.) * 0.5;
  vec3 specular = specular_strength * u_spec;
  
  vec3 result = u_ambi + diffuse + specular;
  f_color = vec4(result, 1.);
}