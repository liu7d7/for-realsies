using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Penki.Client.Engine;
using Vector3 = System.Numerics.Vector3;

namespace Penki.Client.Game;

public class Player : Entity
{
  private static readonly Lazy<Model> _model =
    new(() => new Model(@"Res\Models\HanaSleeveHack.obj"));
  
  public readonly Camera Cam = new Camera();

  public override Vec3 Pos
  {
    get => _handle.GetPos(World.Sim) - Vec3.UnitY * 2.5f;
    set => _handle.SetPos(World.Sim, value + Vec3.UnitY * 2.5f);
  }

  public override Vec3 Vel
  {
    get => _handle.GetVel(World.Sim); 
    set => _handle.SetVel(World.Sim, value);
  }

  private float _bodyYaw;

  private readonly BodyHandle _handle;
  
  public Player(World world) : base(world)
  {
    _handle =
      World.Sim.Bodies.Add(
        BodyDescription.CreateDynamic(
          new Vector3(5, 8, 5),
          new Capsule(1f, 3).ComputeInertia(3f),
          World.Sim.Shapes.Add(new Capsule(1f, 3)),
          0.001f));
  }

  public override void Draw(Mat4 model, RenderSource source)
  {
    _bodyYaw = _bodyYaw.AngleLerp(float.Atan2(Vel.Z, Vel.X).Deg(), 0.2f);
    
    model.Translate(Pos); 
    model.Rotate(Vec3.UnitY, -_bodyYaw + 90);
    
    _model.Get.Draw(model, source);
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

    _handle.SetOrient(World.Sim, Quaternion.Identity);

    Cam.Pos = Pos;
  }
}