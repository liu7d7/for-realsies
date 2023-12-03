using OpenTK.Mathematics;
using Penki.Client.Engine;
using Penki.Game;

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

  private void GenerateChunks()
  {
    var chunkPos = Cam.Pos.ToChunk();
    for (int i = -5; i <= 5; i++)
    for (int j = -5; j <= 5; j++)
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
    for (int i = -5; i <= 5; i++)
    for (int j = -5; j <= 5; j++)
    {
      var final = chunkPos + new Vec2i(i, j);
      _chunks[final].Draw();
    }

    foreach (var it in _entities.Where(it => !(it.Pos.Dist(Cam.Pos) > 100)))
    {
      it.Draw(Mat4.Identity);
    }
  }

  public void Tick()
  {
    foreach (var it in _entities.Where(it => !(it.Pos.Dist(Cam.Pos) > 100)))
    {
      it.Tick();
    }
  }
}