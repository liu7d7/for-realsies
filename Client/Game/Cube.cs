﻿using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using Penki.Client.Engine;
using Shapes = Penki.Client.Engine.Shapes;

namespace Penki.Client.Game;

public class Cube : Entity
{
  public override Vec3 Pos
  {
    get => _handle.GetPos(World.Sim);
    set => _handle.SetPos(World.Sim, value);
  }

  public override Vec3 Vel
  {
    get => _handle.GetPos(World.Sim); 
    set => _handle.SetPos(World.Sim, value);
  }

  public Quaternion Orient
  {
    get => _handle.GetOrient(World.Sim);
    set => _handle.SetOrient(World.Sim, value);
  }

  private static readonly Lazy<InstancedModel> _cube =
    new(() => new InstancedModel(new Model(@"Res\Models\Cube.obj")));

  private readonly BodyHandle _handle;

  public Cube(World world, Vec3 pos) : base(world)
  {
    _handle =
      world.Sim.Bodies.Add(
        BodyDescription.CreateDynamic(
          new RigidPose(pos.ToNumerics()),
          new Box(2, 2, 2).ComputeInertia(1),
          Shapes.Cube[(world, 2)],
          0.01f));
  }
  
  public override void Draw(Mat4 model, RenderSource source)
  {
    model.Translate(Pos);
    model.Rotate(Orient);
    
    _cube.Get.Add(model);
  }

  public override void Tick(float dt)
  {
    
  }
}