#version 460

layout(location = 0) in vec3 v_pos;
layout(location = 1) in vec2 v_uv;
layout(location = 2) in vec3 v_norm;

layout(location = 0) out vec4 f_color;
layout(location = 1) out highp uint f_id;
layout(location = 2) out vec4 f_norm;

uniform vec3 u_eye;
uniform vec3 u_ambi;
uniform vec3 u_diff;
uniform vec3 u_spec;
uniform float u_shine;
uniform bool u_has_norm_tex;
uniform sampler2D u_norm_tex;
uniform highp uint u_id;

const vec3 light_dir = vec3(-1, 2, -1);

vec3 to_hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = pow(10., -10.);
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 to_rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main() {
  vec3 norm = u_has_norm_tex ? normalize(2 * (texture(u_norm_tex, v_uv).rgb - 0.5)) : normalize(v_norm);
  
  float diffuse_strength = max(dot(norm, light_dir), 0.);
  vec3 diffuse = diffuse_strength * u_diff;
  
  vec3 view_dir = normalize(u_eye - v_pos);
  vec3 reflect_dir = normalize(reflect(-light_dir, norm));
  float specular_strength = pow(max(dot(view_dir, reflect_dir), 0.), u_shine) * 0.5;
  vec3 specular = specular_strength * u_spec;
  
  vec3 hsv = to_hsv(u_ambi + diffuse + specular);
  hsv.z = ceil(hsv.z * 3.) / 3.;
  vec3 result = to_rgb(hsv);
  f_color = vec4(result, 1.);
  f_id = u_id;
  f_norm = vec4(norm, 0.);
}