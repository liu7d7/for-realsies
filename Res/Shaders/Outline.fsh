#version 460

layout(location = 0) in vec2 v_uv;

layout(location = 0) out vec4 f_color;

uniform sampler2D u_tex_col;
uniform highp usampler2D u_tex_id;
uniform sampler2D u_tex_norm;
uniform sampler2D u_tex_depth;
uniform vec2 u_one_texel;
uniform float u_z_near;
uniform float u_z_far;
uniform vec3 u_front;

const int size = 3;

float linearize_depth(float d) {
  float z = d * 2.0 - 1.0;
  return (2.0 * u_z_near * u_z_far) / (u_z_far + u_z_near - z * (u_z_far - u_z_near));	
}

void main() {
  highp uint this_pix_id = texture(u_tex_id, v_uv).r;
  vec3 this_pix_norm = texture(u_tex_norm, v_uv).rgb;
  float this_pix_depth = texture(u_tex_depth, v_uv).r;
  float depth_mul = pow(10, mix(0, 1, pow(1 - abs(dot(this_pix_norm, u_front)), 4.)));
  bool found = false;
  for (int i = -size; i <= size; i++) {
    for (int j = -size; j <= size; j++) {
      if (i * i + j * j > size * size) continue;
      highp uint other_pix_id = texture(u_tex_id, v_uv + u_one_texel * vec2(i, j)).r;
      vec3 other_pix_norm = texture(u_tex_norm, v_uv + u_one_texel * vec2(i, j)).rgb;
      float other_pix_depth = texture(u_tex_depth, v_uv + u_one_texel * vec2(i, j)).r;
      if (this_pix_id != other_pix_id) {
        found = true;
        break;
      }
      
      if (dot(normalize(this_pix_norm), normalize(other_pix_norm)) < 0.9) {
        found = true;
        break;
      }
      
      if (abs(linearize_depth(this_pix_depth) - linearize_depth(other_pix_depth)) > 0.5 * depth_mul) {
        found = true;
        break;
      }
    }
    
    if (found) break;
  }
 

  if (found) {
    f_color = vec4(vec3(0.), 1.);
    return;
  }
  
  f_color = texture(u_tex_col, v_uv);
}