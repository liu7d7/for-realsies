using Penki.Client.Engine;
using Penki.Client.GLU;
using SimplexNoise;

namespace Penki.Client.Game;

public class Chunk
{
  public const int Size = 32;

  public readonly Vec2i Pos;
  private readonly Vao _vao;
  private readonly Buf _vbo;
  private readonly Buf _ibo;
  private readonly List<(Vec3, Vec3)> _grass = new();

  public Chunk(Vec2i pos)
  {
    Pos = pos;
    _vbo = new Buf(BufType.ArrayBuffer);
    _ibo = new Buf(BufType.ElementArrayBuffer);
    Build();
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
    for (int i = -1; i < Size + 1; i++)
    for (int j = -1; j < Size + 1; j++)
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
          verts[i * (Size + 1) + j].Norm += Vec3.Cross(b - a, c - a);
        }
        
        if (i + off1.X >= 0 && i + off1.X < Size + 1 && j + off1.Y >= 0 && j + off1.Y < Size + 1)
        {
          verts[(i + off1.X) * (Size + 1) + j + off1.Y].Norm +=
            Vec3.Cross(b - a, c - a);
        }
        
        if (i + off2.X >= 0 && i + off2.X < Size + 1 && j + off2.Y >= 0 && j + off2.Y < Size + 1)
        {
          verts[(i + off2.X) * (Size + 1) + j + off2.Y].Norm +=
            Vec3.Cross(b - a, c - a);
        }
      }
    }

    for (int i = 0; i < verts.Length; i++)
    {
      verts[i].Norm = -verts[i].Norm.Normalized();
    }
  }

  private Vec3 GetPos(int offX, int offZ)
  {
    var basePos = new Vec3(Pos.X * Size + offX, 0, Pos.Y * Size + offZ);
    var height = Noise.CalcPixel2D((int)basePos.X, (int)basePos.Z, 0.03f) / 255.0f * 3;
    return basePos + new Vec3(0, height, 0);
  }

  private void Build()
  {
    var verts = new ObjVtx[(Size + 1) * (Size + 1)];
    for (int i = 0; i < Size + 1; i++)
    for (int j = 0; j < Size + 1; j++)
    {
      verts[i * (Size + 1) + j] = 
        new ObjVtx { Pos = GetPos(i, j) };
    }

    BuildNormals(verts);
    
    for (int i = 0; i < Size + 1; i++)
    for (int j = 0; j < Size + 1; j++)
    {
      if (Random.Shared.NextSingle() < 0.01)
      {
        _grass.Add((
          verts[i * (Size + 1) + j].Pos,
          verts[i * (Size + 1) + j].Norm));
      } 
    }

    _vbo.Data(BufUsage.StaticDraw, verts);
    _ibo.Data(BufUsage.StaticDraw, Utils.QuadIndices(Size, Size));
  }
  
  private static readonly Material _mat = new Material
  {
    Ambi = DreamyHaze.Colors[0] * 0.05f,
    Diff = DreamyHaze.Colors[7] * 0.3f,
    Spec = Vec3.Zero,
    Normals = -1,
    Alpha = -1
  };

  private static readonly uint _rand = (uint)Random.Shared.Next();

  private static readonly Lazy<Model> _grassModel =
    new(() => new Model(@"Res\Models\Grass.obj"));

  public void Draw()
  {
    var shader = (Shader)Model.Shader;
    shader.Bind()
      .Defaults()
      .Uint("u_id", _rand)
      .Mat4("u_model", Mat4.Identity)
      .Mat(_mat);
    _vao.Draw(PrimType.Triangles);

    // foreach (var it in _grass)
    // {
    //   var model = Mat4.Identity;
    //   model = model.ChangeAxis(it.Item2, 1);
    //   model *= Mat4.CreateScale(0.1f);
    //   model *= Mat4.CreateTranslation(it.Item1);
    //
    //   _grassModel.Get.Draw(model);
    // }
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