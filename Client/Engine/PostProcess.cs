using OpenTK.Graphics.OpenGL4;
using Penki.Client.GLU;

namespace Penki.Client.Engine;

public static class PostProcess
{
  private static readonly Lazy<(Vao, Buf)> _quad = new(() =>
  {
    var vbo =
      new Buf(BufType.ArrayBuffer)
        .Data(BufUsage.StaticDraw, new[]
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
  
  private static readonly Memo<Vao, (Vec2i, Vec2i)> _anyQuad = new(tlbr =>
  {
    var ((v00, v01), (v10, v11), (v20, v21), (v30, v31)) = (
      (new Vec2(tlbr.Item1.X, tlbr.Item1.Y), new Vec2(0, 1)),
      (new Vec2(tlbr.Item2.X, tlbr.Item1.Y), new Vec2(1, 1)),
      (new Vec2(tlbr.Item2.X, tlbr.Item2.Y), new Vec2(1, 0)),
      (new Vec2(tlbr.Item1.X, tlbr.Item2.Y), new Vec2(0, 0)));
    
    var vbo =
      new Buf(BufType.ArrayBuffer)
        .Data(BufUsage.StaticDraw, new[]
        {
          v00, v01, v10, v11, v20, v21, v20, v21, v30, v31, v00, v01
        }.AsSpan());

    var vao = new Vao(vbo, null, Vao.Attrib.Float2, Vao.Attrib.Float2);

    return vao;
  });

  private static readonly Lazy<Shader> _blit =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Blit.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Blit.fsh")));

  public static void Blit(Vec2i tl, Vec2i br)
  {
    _blit.Get.Bind().Defaults(RenderSource.World, threeD: false);
    _anyQuad[(tl, br)].Draw(PrimType.Triangles);
  }
}