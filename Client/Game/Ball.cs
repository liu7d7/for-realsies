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
    get => Penki.Simulation.Bodies.GetDescription(_handle).Pose.Position.ToTk();
    set => throw new UnreachableException("Cannot set position of ball"); 
  }

  public override Vec3 Vel
  {
    get => Penki.Simulation.Bodies.GetDescription(_handle).Velocity.Linear.ToTk();
    set => throw new UnreachableException("Cannot set velocity of ball");
  }

  private readonly BodyHandle _handle;

  public Ball(Vec3 pos)
  {
    _handle = Penki.Simulation.Bodies.Add(
      BodyDescription.CreateDynamic(
        pos.ToNumerics(),
        new Sphere(1).ComputeInertia(1),
        Shapes.Sphere[1],
        0.01f));
  }

  public override void Draw(Mat4 model)
  {
    model.Translate(Pos);
    Model.Sphere.Get.Draw(model);
  }

  public override void Tick(float dt)
  {
    
  }
}