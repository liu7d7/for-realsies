using OpenTK.Graphics.OpenGL4;
using Penki.Client.Engine;

namespace Penki.Client.GLU;

public class Shader : IReloadable
{
  public int Id { get; private set; }
  private readonly (ShaderType, string)[] _args;

  public Shader(params (ShaderType, string)[] shaders)
  {
    _args = shaders;
    Id = GL.CreateProgram();

    List<int> toDelete = new();
    foreach (var (a, b) in shaders)
    {
      var text = File.ReadAllText(b);
      var shader = GL.CreateShader(a);
      toDelete.Add(shader);

      GL.ShaderSource(shader, text);
      GL.CompileShader(shader);
      GL.GetShader(shader, ShaderParameter.CompileStatus, out int good);
      if (good != (int)All.True)
      {
        Console.WriteLine($"Compilation of shader {b} failed!");
        throw new Exception(GL.GetShaderInfoLog(shader));
      }
      
      GL.AttachShader(Id, shader);
      
      Reloader.Register(this);
    }

    GL.LinkProgram(Id);
    GL.GetProgram(Id, GetProgramParameterName.LinkStatus, out int yay);
    if (yay != (int)All.True)
    {
      throw new Exception(GL.GetProgramInfoLog(Id));
    }
    
    toDelete.ForEach(GL.DeleteShader);
  }

  public Shader Bind()
  {
    GL.UseProgram(Id);
    return this;
  }

  public Shader Mat4(string name, Mat4 mat)
  {
    GL.UniformMatrix4(GL.GetUniformLocation(Id, name), false, ref mat);
    return this;
  }

  public Shader Int(string name, int val)
  {
    GL.Uniform1(GL.GetUniformLocation(Id, name), val);
    return this;
  }
  
  public Shader Float1(string name, float val)
  {
    GL.Uniform1(GL.GetUniformLocation(Id, name), val);
    return this;
  }
  
  public Shader Float2(string name, Vec2 val)
  {
    GL.Uniform2(GL.GetUniformLocation(Id, name), val);
    return this;
  }
  
  public Shader Float3(string name, Vec3 val)
  {
    GL.Uniform3(GL.GetUniformLocation(Id, name), val);
    return this;
  }

  public Shader Float4(string name, Vec4 val)
  {
    GL.Uniform4(GL.GetUniformLocation(Id, name), val);
    return this;
  }

  public Shader Uint(string name, uint val)
  {
    GL.Uniform1(GL.GetUniformLocation(Id, name), val);
    return this;
  }

  public Shader Mat(Material mat)
  {
    return 
      Float3("u_ambi", mat.Ambi)
      .Float3("u_diff", mat.Diff)
      .Float3("u_spec", mat.Spec)
      .Float1("u_shine", mat.Shine)
      .Int("u_has_norm_tex", mat.Normals == -1 ? 0 : 1)
      .Int("u_norm_tex", 0);
  }

  public int ReloadId { get; set; }
  public void Reload()
  {
    GL.DeleteProgram(Id);
    
    var shader = new Shader(_args);
    Reloader.Deregister(shader);
    Id = shader.Id;
  }
}