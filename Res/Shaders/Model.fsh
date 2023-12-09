#version 460

layout(location = 0) in vec3 v_pos;
layout(location = 1) in vec2 v_uv;
layout(location = 2) in vec3 v_norm;

layout(location = 0) out vec4 f_color;
layout(location = 1) out highp uint f_id;
layout(location = 2) out vec4 f_norm;

uniform vec3 u_eye;
uniform vec3 u_dark;
uniform vec3 u_light;
uniform vec3 u_light_model;
uniform float u_shine;
uniform bool u_has_norm_tex;
uniform sampler2D u_norm_tex;
uniform bool u_has_alpha_tex;
uniform sampler2D u_alpha_tex;
uniform highp uint u_id;

const vec3 light_dir = vec3(-1, 2, -1);

vec3 to_hsv(vec3 c) {
  vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
  vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
  vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

  float d = q.x - min(q.w, q.y);
  float e = 1.0e-10;
  return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 to_rgb(vec3 c) {
  vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
  vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
  return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

vec3 light_calc(vec3 N) {
  vec3 L = normalize(light_dir);
  vec3 V = normalize(u_eye - v_pos.xyz);
  vec3 R = reflect(-L, N);
  float lambert = max(dot(N, L), 0.0);
  float specular = pow(max(dot(R, V), 0.0), u_shine);
  float amt = clamp(u_light_model.x + lambert * u_light_model.y + specular * u_light_model.z, 0., 1.);
  return mix(u_dark, u_light, smoothstep(0., 1., amt));
}

void main() {
  if (u_has_alpha_tex && texture(u_alpha_tex, v_uv).r < 0.9999) discard;
  
  vec3 norm = u_has_norm_tex ? normalize(2 * (texture(u_norm_tex, v_uv).rgb - 0.5)) : normalize(v_norm);
  if (!gl_FrontFacing) {
    norm = -norm;
  }
  
  f_color = vec4(light_calc(norm), 1.);
  f_id = u_id;
  f_norm = vec4(norm, 0.);
}