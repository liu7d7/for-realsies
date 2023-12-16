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
    get => _handle.GetPos(World.Sim);
    set => throw new UnreachableException("Cannot set position of ball"); 
  }

  public override Vec3 Vel
  {
    get => _handle.GetVel(World.Sim);
    set => throw new UnreachableException("Cannot set velocity of ball");
  }

  private readonly BodyHandle _handle;

  public static readonly Lazy<InstancedModel> Model =
    new(() => new InstancedModel(new Model(@"Res\Models\Sphere.obj")));

  public Ball(World world, Vec3 pos) : base(world)
  {
    _handle = world.Sim.Bodies.Add(
      BodyDescription.CreateDynamic(
        pos.ToNumerics(),
        new Sphere(1).ComputeInertia(1),
        Shapes.Sphere[(world, 1)],
        0.001f));
  }

  public override void Draw(Mat4 model, RenderSource source)
  {
    model.Translate(Pos);
    Model.Get.Add(model);
  }

  public override void Tick(float dt)
  {
    
  }
}