using System.Buffers;
using Penki.Client.GLU;

namespace Penki.Client.Engine;

using OpenTK.Graphics.OpenGL4;
using StbTrueTypeSharp;

struct FontVtx
{
  public required Vec2 Pos;
  public required Vec2 Uv;
  public required Vec3 Color;

  public static readonly Vao.Attrib[] Attribs =
  {
    Vao.Attrib.Float2,
    Vao.Attrib.Float2,
    Vao.Attrib.Float3
  };
}

public static class Font
{
  private const float Ipw = 1.0f / 2048f;
  private const float Iph = Ipw;
  private static readonly float _ascent;
  private static readonly StbTrueType.stbtt_packedchar[] _chars;

  public static int Height;
  private static readonly Tex Tex;

  private static readonly Lazy<(Vao, Buf)> _vao =
    new(() =>
    {
      var vbo = new Buf(BufferTarget.ArrayBuffer);
      var vao = new Vao(vbo, null, FontVtx.Attribs);
      return (vao, vbo);
    });

  private static readonly Lazy<Shader> _shader =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Font.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Font.fsh")));

  static unsafe Font()
  {
    byte[] buffer = File.ReadAllBytes(@"Res\Fonts\UDDigiKyokashoNB.ttf");
    const int height = 40;
    Height = height;

    var fontInfo = StbTrueType.CreateFont(buffer, 0);

    _chars = new StbTrueType.stbtt_packedchar[256];
    StbTrueType.stbtt_pack_context packContext = new();

    byte[] bitmap = new byte[2048 * 2048];
    fixed (byte* dat = bitmap)
    {
      StbTrueType.stbtt_PackBegin(packContext, dat, 2048, 2048, 0, 1, null);
    

      StbTrueType.stbtt_PackSetOversampling(packContext, 8, 8);
      fixed (byte* font = buffer)
      {
        fixed (StbTrueType.stbtt_packedchar* c = _chars)
        {
          StbTrueType.stbtt_PackFontRange(packContext, font, 0, height, 32, 256, c);
        }
      }

      StbTrueType.stbtt_PackEnd(packContext);
    }

    int asc;
    StbTrueType.stbtt_GetFontVMetrics(fontInfo, &asc, null, null);
    _ascent = asc * StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, height);

    Tex = new Tex(bitmap, TexConf.R8((2048, 2048)));
  }

  public static void Bind()
  {
    Tex.Bind(TextureUnit.Texture0);
  }

  public static void Draw(string text, float x, float y, Vec3 color, bool shadow, float scale = 1.0f)
  {
    int length = text.Length;
    float drawX = x;
    float drawY = y + _ascent * scale;

    var stride = shadow ? 12 : 6;
    var size = text.Length * stride;
    var verts = ArrayPool<FontVtx>.Shared.Rent(size);
    for (int i = 0; i < length; i++)
    {
      char charCode = text[i];

      if (charCode < 32 || charCode > 32 + 256) charCode = ' ';

      var c = _chars[charCode - 32];

      float dxs = drawX + c.xoff * scale;
      float dys = drawY + c.yoff * scale;
      float dx1S = drawX + c.xoff2 * scale;
      float dy1S = drawY + c.yoff2 * scale;

      var bas = i * stride;

      if (shadow)
      {
        var j0 = new FontVtx
        {
          Pos = (dxs + 1, dys + 1),
          Uv = (c.x0 * Ipw, c.y0 * Iph),
          Color = color * 0.125f
        };

        var j1 = new FontVtx
        {
          Pos = (dxs + 1, dy1S + 1),
          Uv = (c.x0 * Ipw, c.y1 * Iph),
          Color = color * 0.125f
        };

        var j2 = new FontVtx
        {
          Pos = (dx1S + 1, dy1S + 1),
          Uv = (c.x1 * Ipw, c.y1 * Iph),
          Color = color * 0.125f
        };

        var j3 = new FontVtx
        {
          Pos = (dx1S + 1, dys + 1),
          Uv = (c.x1 * Ipw, c.y0 * Iph),
          Color = color * 0.125f
        };

        verts[bas + 0] = j0;
        verts[bas + 1] = j1;
        verts[bas + 2] = j2;
        verts[bas + 3] = j2;
        verts[bas + 4] = j3;
        verts[bas + 5] = j0;

        bas += 6;
      }
      
      var i0 = new FontVtx
      {
        Pos = (dxs, dys),
        Uv = (c.x0 * Ipw, c.y0 * Iph),
        Color = color
      };

      var i1 = new FontVtx
      {
        Pos = (dxs, dy1S),
        Uv = (c.x0 * Ipw, c.y1 * Iph),
        Color = color
      };

      var i2 = new FontVtx
      {
        Pos = (dx1S, dy1S),
        Uv = (c.x1 * Ipw, c.y1 * Iph),
        Color = color
      };

      var i3 = new FontVtx
      {
        Pos = (dx1S, dys),
        Uv = (c.x1 * Ipw, c.y0 * Iph),
        Color = color
      };

      verts[bas + 0] = i0;
      verts[bas + 1] = i1;
      verts[bas + 2] = i2;
      verts[bas + 3] = i2; 
      verts[bas + 4] = i3;
      verts[bas + 5] = i0;

      drawX += c.xadvance * scale;
      drawX -= 0.4f * scale;
    }

    Tex.Bind(TextureUnit.Texture0);
    _shader.Get.Bind().Defaults(RenderSource.World, threeD: false).Int("u_tex", 0);
    _vao.Get.Item2.Data(BufUsage.DynamicDraw, verts.AsSpan(), size);
    _vao.Get.Item1.Draw(PrimitiveType.Triangles);
    
    verts.Return();
  }

  public static float GetWidth(string text, float scale = 1.0f)
  {
    int length = text.Length;
    float width = 0;
    for (int i = 0; i < length; i++)
    {
      char charCode = text[i];
      char previous = i > 0 ? text[i - 1] : ' ';
      if (previous == '\u00a7') continue;

      if (charCode < 32 || charCode > 32 + 256) charCode = ' ';

      StbTrueType.stbtt_packedchar c = _chars[charCode - 32];

      width += c.xadvance * scale;
      width -= 0.4f * scale;
    }

    width += 0.4f * scale;

    return width;
  }

  public static float GetHeight(float scale = 1.0f)
  {
    return _ascent * scale;
  }
}