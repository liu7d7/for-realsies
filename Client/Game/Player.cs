using System.Diagnostics;
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

  private static readonly Lazy<Model> _model =
    new(() => new Model(@"Res\Models\Hana.obj"));

  private static readonly Lazy<Shader> _capeShader = 
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Model.fsh")));
  
  private static readonly Lazy<Shader> _capeDepth = 
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Depth.fsh")));
  
  private static readonly Lazy<Shader> _capeWireframe =
    new(() => 
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Cape.vsh"),
        (ShaderType.GeometryShader, @"Res\Shaders\Wireframe.gsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Lines.fsh")));

  private static readonly Memo<Shader, RenderSource> _shader =
    new(source => source switch
    {
      RenderSource.Lightmap => _capeDepth.Get,
      RenderSource.World when !Penki.Wireframe => _capeShader,
      RenderSource.World when Penki.Wireframe => _capeWireframe,
      _ => throw new UnreachableException("uh")
    });

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
    get => _handle.GetPos() - Vec3.UnitY * 3f;
    set => _handle.SetPos(value + Vec3.UnitY * 3f);
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
        new Capsule(1.5f, 3).ComputeInertia(1f),
        Penki.Simulation.Shapes.Add(new Capsule(1.5f, 3)),
        0.001f));

  public override void Draw(Mat4 model, RenderSource source)
  {
    _bodyYaw = _bodyYaw.AngleLerp(float.Atan2(Vel.Z, Vel.X).Deg(), 0.2f);
    
    model.Translate(Pos); 
    model.Rotate(Vec3.UnitY, -_bodyYaw + 90);
    
    _model.Get.Draw(model, source);
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
    if (args is { Key: Keys.Space, IsRepeat: false })
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

    Vel = Vec3.Lerp(Vel * (1, 0, 1), Vec3.Zero, 0.01f) + Vec3.UnitY * Vel.Y;
    Vel += (dir.X * Cam.Right.Normalized() +
            dir.Z * (Cam.Front * new Vec3(1, 0, 1)).Normalized()).NormalizedSafe() * 7.6f * dt;

    _handle.SetOrient(Quaternion.Identity);

    Cam.Pos = Pos;
  }
}