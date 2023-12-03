using Penki.Client.GLU;

namespace Penki.Client.Engine;

public static class PostProcess
{
  private static readonly Lazy<(Vao, Buf)> _quad = new(() =>
  {
    var vbo =
      new Buf(BufType.ArrayBuffer)
        .Data(BufUsage.StaticDraw, new Vec2[]
        {
          new Vec2(-1, -1),
          new Vec2(-1, 1),
          new Vec2(1, 1),
          new Vec2(1, 1),
          new Vec2(1, -1),
          new Vec2(-1, -1)
        }.AsSpan());

    var vao = new Vao(vbo, null, Vao.Attrib.Float2);

    return (vao, vbo);
  });
  
  public static void DrawFullscreenQuad()
  {
    _quad.Get.Item1.Draw(PrimType.Triangles);
  } 
}