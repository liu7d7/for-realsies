using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using OpenTK.Mathematics;
using Penki.Client.Engine;

namespace Penki.Client.Game;

public class World
{
  private readonly Dictionary<Vector2i, Chunk> _chunks = new();
  private readonly List<Entity> _entities = new();
  
  public readonly Simulation Sim = 
    Simulation.Create(
      Penki.BufferPool, 
      new DemoNarrowPhaseCallbacks(new SpringSettings(80, 0.8f)), 
      new DemoPoseIntegratorCallbacks(new System.Numerics.Vector3(0, -10, 0)),
      new SolveDescription(8, 4));

  public readonly ThreadDispatcher ThreadDispatcher =
    new ThreadDispatcher(Environment.ProcessorCount);

  public void Add(Entity entity)
  {
    _entities.Add(entity);
  }

  private const int RenderDistance = 6;

  private void GenerateChunks()
  {
    var chunkPos = Penki.Cam.Pos.ToChunk();
    for (int i = -RenderDistance * 2; i <= RenderDistance * 2; i++)
    for (int j = -RenderDistance * 2; j <= RenderDistance * 2; j++)
    {
      var final = chunkPos + new Vec2i(i, j);
      if (!_chunks.ContainsKey(final))
      {
        _chunks[final] = new Chunk(final, this);
      }
    }
  }

  public void Draw(RenderSource source)
  {
    GenerateChunks();
    
    var chunkPos = Penki.Cam.Pos.ToChunk();
    for (int i = -RenderDistance; i <= RenderDistance; i++)
    for (int j = -RenderDistance; j <= RenderDistance; j++)
    {
      var final = chunkPos + new Vec2i(i, j);
      _chunks[final].Draw(source);
    }

    foreach (var it in _entities)
    {
      if (it.Pos.Dist(Penki.Cam.Pos) > 100) continue;
      
      it.Draw(Mat4.Identity, source);
    }
  }

  public void Tick(float dt)
  {
    Sim.Timestep(1.0f / 50f, ThreadDispatcher);
    
    foreach (var it in _entities)
    {
      if (it.Pos.Dist(Penki.Cam.Pos) > 100) continue;
      
      it.Tick(dt);
    }
  }
}