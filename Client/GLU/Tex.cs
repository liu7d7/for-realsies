using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace Penki.Client.GLU;

public class Tex
{
  public readonly int Id;
  public readonly int Width, Height;

  public Tex(string path)
  {
    GL.GenTextures(1, out Id);
    
    StbImage.stbi_set_flip_vertically_on_load(1);
    var image = 
      ImageResult.FromStream(
        File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

    (Width, Height) = (image.Width, image.Height);
    
    GL.BindTexture(TexType.Texture2D, Id);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureWrapS, (int)All.MirroredRepeat);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureWrapT, (int)All.MirroredRepeat);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
    GL.TexImage2D(TexType.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height,
      0, PixFmt.Rgba, PixType.UnsignedByte, image.Data);
  }

  public Tex(byte[] data, TexConf conf)
  {
    GL.GenTextures(1, out Id);
    (Width, Height) = (conf.Width, conf.Height);
    
    GL.BindTexture(TexType.Texture2D, Id);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureWrapS, (int)All.MirroredRepeat);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureWrapT, (int)All.MirroredRepeat);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureMinFilter, (int)conf.Min);
    GL.TexParameter(TexType.Texture2D, TextureParameterName.TextureMagFilter, (int)conf.Mag);
    GL.TexImage2D(TexType.Texture2D, 0, conf.IntFmt, Width, Height,
      0, conf.Fmt, PixType.UnsignedByte, data);
  }

  public Tex Bind(TexUnit unit)
  {
    GL.ActiveTexture(unit);
    GL.BindTexture(TexType.Texture2D, Id);
    return this;
  }
}