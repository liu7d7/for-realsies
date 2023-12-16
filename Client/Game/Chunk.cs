using System.Buffers;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Graphics.OpenGL;
using Penki.Client.Engine;
using Penki.Client.GLU;
using SimplexNoise;

namespace Penki.Client.Game;

public class Chunk
{
  public const int Size = 32;
  public const int Quality = 16;
  public const int Ratio = Size / Quality;

  public readonly Vec2i Pos;
  private readonly Vao _vao;
  private readonly Buf _vbo;
  private readonly Buf _ibo;

  public Chunk(Vec2i pos, World world)
  {
    Pos = pos;
    _vbo = new Buf(BufType.ArrayBuffer);
    _ibo = new Buf(BufType.ElementArrayBuffer);
    Build(world);
    _vao = new Vao(_vbo, _ibo, ObjVtx.Attribs);
  }

  private static readonly Vec2i[] _normalOffsets =
  {
    new Vec2i(1, 0),
    new Vec2i(1, 1),
    new Vec2i(0, 1)
  };

  private void BuildNormals(ObjVtx[] verts)
  {
    for (int i = -1; i < Quality + 1; i++)
    for (int j = -1; j < Quality + 1; j++)
    {
      var a = GetPos(i, j);
      for (int k = 0; k < _normalOffsets.Length - 1; k++)
      {
        var (off1, off2) = (_normalOffsets[k], _normalOffsets[k + 1]);
        var (b, c) =
          (GetPos(i + off1.X, j + off1.Y),
            GetPos(i + off2.X, j + off2.Y));

        if (i >= 0 && j >= 0)
        {
          verts[i * (Quality + 1) + j].Norm += Vec3.Cross(b - a, c - a);
        }
        
        if (i + off1.X >= 0 && i + off1.X < Quality + 1 && j + off1.Y >= 0 && j + off1.Y < Quality + 1)
        {
          verts[(i + off1.X) * (Quality + 1) + j + off1.Y].Norm +=
            Vec3.Cross(b - a, c - a);
        }
        
        if (i + off2.X >= 0 && i + off2.X < Quality + 1 && j + off2.Y >= 0 && j + off2.Y < Quality + 1)
        {
          verts[(i + off2.X) * (Quality + 1) + j + off2.Y].Norm +=
            Vec3.Cross(b - a, c - a);
        }
      }
    }

    for (int i = 0; i < verts.Length; i++)
    {
      verts[i].Norm = -verts[i].Norm.Normalized();
    }
  }

  public static float HeightAtBiLerp(Vec3 pos)
  {
    int x1 = (int)MathF.Floor(pos.X);
    int z1 = (int)MathF.Floor(pos.Z);

    float v00 = HeightAt(new Vec3(x1, 0, z1));
    float v10 = HeightAt(new Vec3(x1 + 1, 0, z1));
    float v11 = HeightAt(new Vec3(x1 + 1, 0, z1 + 1));
    float v01 = HeightAt(new Vec3(x1, 0, z1 + 1));
    float x = pos.X - x1;
    float z = pos.Z - z1;
    
    return (1 - x) * (1 - z) * v00 + x * (1 - z) * v10 + (1 - x) * z * v01 + x * z * v11;
  }

  private static float BumpinessAt(Vec3 pos)
  {
    return -(Noise.CalcPixel2D((int)pos.X, (int)pos.Z, 0.005f) / 255.0f - 0.5f) * 2 * 8f + 4f;
  }

  private static float DivAt(Vec3 pos)
  {
    return (Noise.CalcPixel2D((int)pos.X, (int)pos.Z, 0.001f) / 255.0f - 0.5f) * 2 * 0.01f + 0.02f;
  }

  public static float HeightAt(Vec3 pos)
  {
    return Noise.CalcPixel2D((int)pos.X, (int)pos.Z, DivAt(pos)) / 255.0f * BumpinessAt(pos);
  }

  private Vec3 GetPos(int offX, int offZ)
  {
    var basePos = new Vec3(Pos.X * Size + offX * Ratio, 0, Pos.Y * Size + offZ * Ratio);
    return basePos + new Vec3(0, HeightAt(basePos), 0);
  }

  private void Build(World world)
  {
    const int size = (Quality + 1) * (Quality + 1);
    var verts = ArrayPool<ObjVtx>.Shared.Rent(size);
    for (int i = 0; i < Quality + 1; i++)
    for (int j = 0; j < Quality + 1; j++)
    {
      verts[i * (Quality + 1) + j] = new ObjVtx { Pos = GetPos(i, j) };
    }

    BuildNormals(verts);
    
    _vbo.Data(BufUsage.StaticDraw, verts.AsSpan(), size);
    var (indices, indicesLength) = Utils.QuadIndices(Quality, Quality);
    _ibo.Data(BufUsage.StaticDraw, indices.AsSpan(), indicesLength);

    var tris = Utils.Tris(verts.AsSpan(), indices.AsSpan(), indicesLength, Penki.BufferPool);
    var mesh = new Mesh(tris, Vector3.One, Penki.BufferPool);
    var chunkShape = world.Sim.Shapes.Add(mesh);
    world.Sim.Statics.Add(new StaticDescription(Vector3.Zero, chunkShape));
    
    for (int i = 0; i < 5; i++)
    {
      world.Add(new Ball(world, GetPos(Quality / 2, Quality / 2) + Vec3.UnitY * 12 + new Vec3(Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12))));
      world.Add(new Ball(world, GetPos(Quality / 2, Quality / 2) + Vec3.UnitY * 12 + new Vec3(Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12))));
    }
    
    for (int i = 0; i < 5; i++)
    {
      world.Add(new Cube(world, GetPos(Quality / 2, Quality / 2) + Vec3.UnitY * 12 + new Vec3(Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12))));
      world.Add(new Cube(world, GetPos(Quality / 2, Quality / 2) + Vec3.UnitY * 12 + new Vec3(Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12), Random.Shared.Next(-12, 12))));
    }
    
    verts.Return();
    indices.Return();
  }
  
  private static readonly Material _mat = new Material
  {
    Light = DreamyHaze.Colors[6],
    Dark = DreamyHaze.Colors[0],
    LightModel = (0.0f, 0.8f, 0.0f),
    Normals = -1,
    Alpha = -1
  };

  private static readonly uint _rand = (uint)Random.Shared.Next();

  public void Draw(RenderSource source)
  {
    Model.Shader[source].Bind()
      .Defaults(source)
      .Uint("u_id", _rand)
      .Mat4("u_model", Mat4.Identity)
      .Mat(_mat);
    
    GL.Disable(EnableCap.CullFace);
    _vao.Draw(PrimType.Triangles);
    GL.Enable(EnableCap.CullFace);
  }
}

public static class ChunkExtensions
{
  public static Vec2i ToChunk(this Vec3 worldPos)
  {
    return new Vec2i(
      (int)worldPos.X / Chunk.Size,
      (int)worldPos.Z / Chunk.Size);
  }
}