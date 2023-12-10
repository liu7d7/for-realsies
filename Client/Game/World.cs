using OpenTK.Mathematics;
using Penki.Client.Engine;

namespace Penki.Client.Game;

public class World
{
  private readonly Dictionary<Vector2i, Chunk> _chunks = new();
  private readonly List<Entity> _entities = new();
  public readonly Camera Cam;

  public World(Camera cam)
  {
    Cam = cam;
  }

  public void Add(Entity entity)
  {
    _entities.Add(entity);
  }

  private const int RenderDistance = 3;

  private void GenerateChunks()
  {
    var chunkPos = Cam.Pos.ToChunk();
    for (int i = -RenderDistance * 3; i <= RenderDistance * 3; i++)
    for (int j = -RenderDistance * 3; j <= RenderDistance * 3; j++)
    {
      var final = chunkPos + new Vec2i(i, j);
      if (!_chunks.ContainsKey(final))
      {
        _chunks[final] = new Chunk(final);
      }
    }
  }

  public void Draw()
  {
    GenerateChunks();
    
    var chunkPos = Cam.Pos.ToChunk();
    for (int i = -RenderDistance; i <= RenderDistance; i++)
    for (int j = -RenderDistance; j <= RenderDistance; j++)
    {
      var final = chunkPos + new Vec2i(i, j);
      _chunks[final].Draw();
    }

    foreach (var it in _entities)
    {
      if (it.Pos.Dist(Cam.Pos) > 100) continue;
      
      it.Draw(Mat4.Identity);
    }
  }

  public void Tick(float dt)
  {
    foreach (var it in _entities)
    {
      if (it.Pos.Dist(Cam.Pos) > 100) continue;
      
      it.Tick(dt);
    }
  }
}