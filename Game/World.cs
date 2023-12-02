using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Penki.Client.Engine;

namespace Penki.Game;

public class World
{
  private readonly Dictionary<Vector2i, Chunk> _chunks = new();
  private readonly Camera _camera;

  public World(Camera camera)
  {
    _camera = camera;
  }

  private void GenerateChunks()
  {
    var chunkPos = _camera.Pos.ToChunk();
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
    
    var chunkPos = _camera.Pos.ToChunk();
    for (int i = -5; i <= 5; i++)
    for (int j = -5; j <= 5; j++)
    {
      var final = chunkPos + new Vec2i(i, j);
      _chunks[final].Draw(_camera.Eye);
    }
  }
}