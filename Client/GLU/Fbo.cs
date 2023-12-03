using System.Diagnostics;
using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Penki.Client.GLU;

using FboAttachment = Triple<FboComp, TexConf, int>;

public record struct TexConf(
  PixIntFmt IntFmt,
  PixFmt Fmt,
  PixType Type,
  MinFilter Min,
  MagFilter Mag,
  int Width,
  int Height)
{
  public static TexConf Depth24(Vec2i size)
  {
    return new TexConf(
      PixIntFmt.DepthComponent24,
      PixFmt.DepthComponent,
      PixType.Float,
      MinFilter.Nearest,
      MagFilter.Nearest,
      size.X,
      size.Y
    );
  }

  public static TexConf Rgba32(Vec2i size)
  {
    return new TexConf(
      PixIntFmt.Rgba32f,
      PixFmt.Rgba,
      PixType.Float,
      MinFilter.Linear,
      MagFilter.Linear,
      size.X,
      size.Y
    );
  }
  
  public static TexConf R32(Vec2i size)
  {
    return new TexConf(
      PixIntFmt.R32ui,
      PixFmt.RedInteger,
      PixType.UnsignedInt,
      MinFilter.Linear,
      MagFilter.Linear,
      size.X,
      size.Y
    );
  }
}

public class Fbo
{
  private readonly List<FboAttachment> _comps;
  public readonly int Id;

  public Fbo(params (FramebufferAttachment, TexConf)[] comps)
  {
    _comps = 
      comps.Select(
        it => new FboAttachment(it.Item1, it.Item2, MakeTex(it.Item2)))
        .ToList();
    
    GL.CreateFramebuffers(1, out Id);
    Bind();
    
    foreach (var (a, _, c) in _comps)
    {
      GL.FramebufferTexture2D(
        FramebufferTarget.Framebuffer, a, TexType.Texture2D, c, 0);
    }
  }

  public Fbo Resize(params (FramebufferAttachment, Vector2i)[] sizes)
  {
    foreach (var (a, b) in sizes)
    {
      var it = 
        _comps.First(it => it.A == a) 
        ?? throw new NullReferenceException($"Could not find {a}");

      it.B = it.B with { Width = b.X, Height = b.Y };
      
      GL.DeleteTextures(1, ref it.C);
      it.C = MakeTex(it.B);
      
      GL.FramebufferTexture2D(
        FramebufferTarget.Framebuffer, it.A, TexType.Texture2D, it.C, 0);
    }

    return this;
  }

  public Fbo BindTex(FboComp comp, int unit)
  {
    GL.ActiveTexture(TextureUnit.Texture0 + unit);
    GL.BindTexture(TextureTarget.Texture2D, _comps.Find(it => it.A == comp)!.C);
    return this;
  }
  
  public int GetTex(FboComp comp)
  {
    return _comps.Find(it => it.A == comp)!.C;
  }

  public Fbo Bind()
  {
    GL.BindFramebuffer(FramebufferTarget.Framebuffer, Id);
    return this;
  }

  public static void Bind0()
  {
    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
  }

  public Fbo Clear(ClearBuffer buf, int drawBuffer, Span<uint> value)
  {
    Debug.Assert(buf == ClearBuffer.Color && value.Length == 4 || buf == ClearBuffer.Depth && value.Length == 1);
    GL.ClearBuffer(buf, drawBuffer, ref value[0]);
    return this;
  }
  
  public Fbo Clear(ClearBuffer buf, int drawBuffer, Span<int> value)
  {
    Debug.Assert(buf == ClearBuffer.Color && value.Length == 4 || buf == ClearBuffer.Depth && value.Length == 1);
    GL.ClearBuffer(buf, drawBuffer, ref value[0]);
    return this;
  }
  
  public Fbo Clear(ClearBuffer buf, int drawBuffer, Span<float> value)
  {
    Debug.Assert(buf == ClearBuffer.Color && value.Length == 4 || buf == ClearBuffer.Depth && value.Length == 1);
    GL.ClearBuffer(buf, drawBuffer, ref value[0]);
    return this;
  }

  public Fbo DrawBuffers(params DrawBuffersEnum[] comps)
  {
    GL.NamedFramebufferDrawBuffers(Id, comps.Length, comps);
    return this;
  }

  private static int MakeTex(TexConf it)
  {
    GL.CreateTextures(TexType.Texture2D, 1, out int tex);
    GL.TextureParameter(tex, TexParam.TextureWrapS, (int)All.MirroredRepeat);
    GL.TextureParameter(tex, TexParam.TextureWrapT, (int)All.MirroredRepeat);
    GL.TextureParameter(tex, TexParam.TextureMinFilter, (int)it.Min);
    GL.TextureParameter(tex, TexParam.TextureMinFilter, (int)it.Mag);
    GL.TextureStorage2D(tex, 1, it.IntFmt, it.Width, it.Height);
    return tex;
  }
}