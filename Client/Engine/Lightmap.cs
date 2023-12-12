using OpenTK.Graphics.OpenGL4;
using Penki.Client.GLU;

namespace Penki.Client.Engine;

public class Lightmap
{
  public readonly Fbo Fbo;
  public readonly int Width, Height;
  public const float Near = 1.0f, Far = 200f;

  public readonly Mat4 Proj =
    Mat4.CreateOrthographic(300f, 300f, Near, Far);

  public static Mat4 View =>
    Mat4.LookAt(Penki.Cam.Eye + new Vec3(-1, 2, -1) * 20, Penki.Cam.Eye, Vec3.UnitY);
  
  public Lightmap(int width, int height)
  {
    (Width, Height) = (width, height);
    Fbo = new Fbo((FboComp.DepthAttachment, TexConf.Depth24Linear((width, height))));
    GL.NamedFramebufferDrawBuffer(Fbo.Id, DrawBufferMode.None);
  }

  public void Consume(Action draw)
  {
    GL.Viewport(0, 0, Width, Height);
    Fbo.Bind().Clear(ClearBuffer.Depth, 0, stackalloc float[] { 1 });

    draw();
  }
}