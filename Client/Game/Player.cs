using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Penki.Client.Engine;
using Penki.Client.GLU;
using Vector3 = System.Numerics.Vector3;

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
      var (indices, indicesLength) = Utils.QuadIndices(Layers, Slices);
      ibo.Data(BufUsage.StaticDraw, indices.AsSpan(), indicesLength);
      indices.Return();

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

  private static readonly Lazy<Shader> _capeShader = 
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Model.fsh")));
  
  private static readonly Lazy<Shader> _capeWireframe =
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.GeometryShader, @"Res\Shaders\Wireframe.gsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Lines.fsh")));

  private static Lazy<Shader> Shader =>
    Penki.Wireframe ? _capeWireframe : _capeShader; 

  private static readonly Material _mat =
    new Material
    {
      Light = DreamyHaze.Colors[6],
      Dark = DreamyHaze.Colors[0],
      LightModel = (0.0f, 0.85f, 0.0f),
      Normals = -1,
      Alpha = -1
    };

  private Vec2 _handProgress;

  public readonly Camera Cam = new Camera();

  public override Vec3 Pos
  {
    get => _handle.GetPos() - Vec3.UnitY * 2;
    set => _handle.SetPos(value + Vec3.UnitY * 2);
  }

  public override Vec3 Vel
  {
    get => _handle.GetVel(); 
    set => _handle.SetVel(value);
  }

  private float _bodyYaw;

  private readonly BodyHandle _handle =
    Penki.Simulation.Bodies.Add(
      BodyDescription.CreateDynamic(
        new Vector3(5, 8, 5),
        new Capsule(1, 5).ComputeInertia(100f),
        Penki.Simulation.Shapes.Add(new Capsule(1, 3)),
        0.01f));

  public override void Draw(Mat4 model)
  {
    _bodyYaw = _bodyYaw.AngleLerp(float.Atan2(Vel.Z, Vel.X).Deg(), 0.2f);
    
    model *= Mat4.CreateTranslation(Pos);
    model.Rotate(Vec3.UnitY, 180f - _bodyYaw);
    
    Shader.Get.Bind()
      .Defaults()
      .Mat(_mat)
      .Float1("u_slices", Slices)
      .Float1("u_layers", Layers)
      .Float1("u_start", Start)
      .Float1("u_end", End)
      .Mat4("u_model", model)
      .Uint("u_id", _rand);
    _cape.Get.Item1.Draw(PrimType.Triangles);
    
    DrawHand(0, model);
    DrawHand(1, model);
    
    model.Rotate(Vec3.UnitY, 180f);
    model.Scale(Vec3.One * 0.5f);
    model.Translate(Vec3.UnitY * 2.0f);
    model.Translate(Vec3.UnitX * 0.111f);
    GL.Disable(EnableCap.CullFace);
    _hood.Get.Draw(model);
    GL.Enable(EnableCap.CullFace);
  }

  private static float HandProgressToAngle(float prog)
  {
    var t = (float)GLFW.GetTime() - prog;
    const float swingLength = 0.33f;
    const float halfSwingLength = swingLength / 2f;
    if (t > swingLength) return 0;
    
    if (t > halfSwingLength) 
    {
      return (1 - (t - halfSwingLength) / halfSwingLength) * 60f;
    }

    return t / halfSwingLength * 60f;
  }

  private void DrawHand(int hand, Mat4 model)
  {
    model.Scale(Vec3.One * 0.2f);
    model.Translate(Vec3.UnitY * 1.4f);
    model.Rotate(Vec3.UnitY, HandProgressToAngle(_handProgress[hand]) * (hand - 0.5f) * 2);
    model.Translate(Vec3.UnitZ * -0.7f * (hand - 0.5f) * 2);
    Model.Sphere.Get.Draw(model);
  }

  public bool SwingInProgress => GLFW.GetTime() - _handProgress[0] <= 0.33f ||
                                 GLFW.GetTime() - _handProgress[1] <= 0.33f; 

  public void MouseDown(MouseButtonEventArgs args)
  {
    if (SwingInProgress) return;
    
    switch (args.Button)
    {
      case MouseButton.Left:
        _handProgress[0] = (float)GLFW.GetTime();
        break;
      case MouseButton.Right:
        _handProgress[1] = (float)GLFW.GetTime();
        break;
    }
  }

  public void KeyDown(KeyboardKeyEventArgs args)
  {
    if (args is { Key: Keys.Space })
    {
      Vel = (Vel.X, Vel.Y + 5f, Vel.Z);
    }
  }

  public override void Tick(float dt)
  {
    var dir = Vec3.Zero;
    if (Penki.IsDown(Keys.W)) dir.Z++;
    if (Penki.IsDown(Keys.S)) dir.Z--;
    if (Penki.IsDown(Keys.A)) dir.X--;
    if (Penki.IsDown(Keys.D)) dir.X++;
    if (dir.Length > 0.0001) dir.Normalize();

    Vel += (dir.X * Cam.Right.Normalized() +
            dir.Z * (Cam.Front * new Vec3(1, 0, 1)).Normalized()).NormalizedSafe() * 7.6f * dt;
    
    _handle.SetOrient(Quaternion.Identity);

    Cam.Pos = Pos;
  }
}