using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Penki.Client.Engine;
using Shapes = Penki.Client.Engine.Shapes;

namespace Penki.Client.Game;

public class Ball : Entity
{
  public override Vec3 Pos
  {
    get => _handle?.GetPos(World.Sim) ?? _lastDesc.Pose.Position.ToTk();
    set => throw new UnreachableException("Cannot set position of ball"); 
  }

  public override Vec3 Vel
  {
    get => _handle?.GetVel(World.Sim) ?? Vec3.Zero;
    set => throw new UnreachableException("Cannot set velocity of ball");
  }

  private BodyHandle? _handle;
  private BodyDescription _lastDesc;

  public static readonly Lazy<InstancedModel> Model =
    new(() => new InstancedModel(new Model(@"Res\Models\Sphere.obj")));

  public Ball(World world, Vec3 pos) : base(world)
  {
    _lastDesc = 
      BodyDescription.CreateDynamic(
        pos.ToNumerics(),
        new Sphere(1).ComputeInertia(1),
        Shapes.Sphere[(world, 1)],
        0.001f);
  }

  public override void Draw(Mat4 model, RenderSource source)
  {
    model.Translate(Pos);
    Model.Get.Add(model);
  }

  public override void Tick(float dt)
  {
    
  }

  public override void EnterView()
  {
    _handle = World.Sim.Bodies.Add(_lastDesc);
  }

  public override void ExitView()
  {
    if (_handle is null)
      throw new NullReferenceException("how did this even happen?");
    
    _lastDesc = _handle.Value.GetDesc(World.Sim);
    World.Sim.Bodies.Remove(_handle.Value);
    _handle = null;
  }
}