using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using Penki.Client.Engine;

namespace Penki.Client.Game;

public class Cube(Vec3 pos) : Entity
{
  public override Vec3 Pos
  {
    get => _handle.GetPos();
    set => _handle.SetPos(value);
  }

  public override Vec3 Vel
  {
    get => _handle.GetPos(); 
    set => _handle.SetPos(value);
  }

  public Quaternion Orient
  {
    get => _handle.GetOrient();
    set => _handle.SetOrient(value);
  }

  private static readonly Lazy<Model> _cube =
    new(() => new Model(@"Res\Models\Cube.obj"));
  
  private static readonly Lazy<TypedIndex> _cubeIndex =
    new(() => Penki.Simulation.Shapes.Add(new Box(2, 2, 2)));

  private readonly BodyHandle _handle =
    Penki.Simulation.Bodies.Add(
      BodyDescription.CreateDynamic(
        new RigidPose(pos.ToNumerics()),
        new Box(2, 2, 2).ComputeInertia(1),
        new CollidableDescription(_cubeIndex.Get),
        0.01f));
  
  public override void Draw(Mat4 model, RenderSource source)
  {
    model.Translate(Pos);
    model.Rotate(Orient);
    
    _cube.Get.Draw(model, source);
  }

  public override void Tick(float dt)
  {
    
  }
}