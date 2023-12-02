using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using Penki.Client.Engine;
using Penki.Client.GLU;
using Penki.Game;

namespace Penki.Client.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct CapeVtx
{
  public float Layer;
  public float Slice;

  public static readonly Vao.Attrib[] Attribs =
  {
    Vao.Attrib.Float1,
    Vao.Attrib.Float1
  };
}

public class Player : Entity
{
  private const int Layers = 24;
  private const int Slices = 64;
  private const float Start = -110;
  private const float End = 110;
  
  private static readonly Lazy<(Vao, Buf, Buf)> _dress =
    new(() =>
    {
      var verts = new CapeVtx[(Layers + 1) * (Slices + 1)];
      for (int i = 0; i <= Slices; i++)
      for (int j = 0; j <= Layers; j++)
      {
        verts[i * (Layers + 1) + j].Layer = j;
        verts[i * (Layers + 1) + j].Slice = i;
      }

      var vbo = new Buf(BufType.ArrayBuffer);
      vbo.Data(BufUsage.StaticDraw, verts);

      var ibo = new Buf(BufType.ElementArrayBuffer);
      ibo.Data(BufUsage.StaticDraw, Utils.QuadIndices(Layers, Slices));

      var vao = new Vao(vbo, ibo, CapeVtx.Attribs);

      return (vao, vbo, ibo);
    });

  private static readonly Lazy<Shader> _shader = 
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Model.fsh")));

  private static readonly Material _mat =
    new Material
    {
      Ambi = new Vec3(0.3f, 0.3f, 0.3f),
      Diff = new Vec3(0.3f, 0.3f, 0.3f),
      Spec = Vec3.One * 0.3f,
      Normals = -1
    };
  
  public override void Draw(Mat4 model)
  {
    _shader.Get.Bind()
      .Mat(_mat)
      .Float1("u_slices", Slices)
      .Float1("u_layers", Layers)
      .Float1("u_start", Start)
      .Float1("u_end", End)
      .Defaults();
    _dress.Get.Item1.Draw(PrimType.Triangles);
  }

  public override void Tick()
  {
    
  }
}