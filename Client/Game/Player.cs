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
  private const int Slices = 72;
  private const float Start = -180;
  private const float End = 180;
  
  private static readonly Lazy<(Vao, Buf, Buf)> _cape =
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

  private static readonly uint _rand = (uint)Random.Shared.Next();

  private static readonly Lazy<Model> _hood =
    new(() =>
    {
      var mod = new Model(@"Res\Models\Hood.obj");
      mod.Objs.Find(it => it.Name == "Hood")!.Rand = _rand;
      return mod;
    });

  private static readonly Lazy<Shader> _shader = 
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Model.fsh")));

  private static readonly Lazy<Model> _sphere =
    new(() => new Model(@"Res\Models\Sphere.obj"));

  private static readonly Material _mat =
    new Material
    {
      Light = DreamyHaze.Colors[6],
      Dark = DreamyHaze.Colors[1],
      LightModel = (0.0f, 0.85f, 0.0f),
      Normals = -1,
      Alpha = -1
    };

  public readonly Camera Cam = new Camera();

  public override Vec3 Pos
  {
    get => Cam.Pos;
    set => Cam.Pos = value;
  }
  
  public override void Draw(Mat4 model)
  { 
    model *= Mat4.CreateTranslation(Pos);
    model.Rotate(Vec3.UnitY, 180f - Cam.Yaw);
    
    _shader.Get.Bind()
      .Defaults()
      .Mat(_mat)
      .Float1("u_slices", Slices)
      .Float1("u_layers", Layers)
      .Float1("u_start", Start)
      .Float1("u_end", End)
      .Mat4("u_model", model)
      .Uint("u_id", _rand);
    _cape.Get.Item1.Draw(PrimType.Triangles);
    
    DrawHand(-1, model);
    DrawHand(1, model);
    
    model.Rotate(Vec3.UnitY, 180f);
    model.Scale(Vec3.One * 0.5f);
    model.Translate(Vec3.UnitY * 2.0f);
    model.Translate(Vec3.UnitX * -0.311f);
    _hood.Get.Draw(model);
  }

  private void DrawHand(int mul, Mat4 model)
  {
    model.Scale(Vec3.One * 0.2f);
    model.Translate(Vec3.UnitY * 1.4f);
    model.Translate(Vec3.UnitZ * -0.8f * mul);
    _sphere.Get.Draw(model);
  }

  public override void Tick()
  {
    
  }
}