using OpenTK.Graphics.OpenGL4;
using Penki.Client.GLU;
using StbImageSharp;

namespace Penki.Client.Engine;

public class Skybox
{
  public readonly int Id;

  public Skybox(string path)
  {
    GL.GenTextures(1, out Id);
    GL.BindTexture(TextureTarget.TextureCubeMap, Id);
    
    GL.TexParameter(TextureTarget.TextureCubeMap,
      TextureParameterName.TextureMinFilter, (int)All.Linear);
    GL.TexParameter(TextureTarget.TextureCubeMap,
      TextureParameterName.TextureMagFilter, (int)All.Linear);
    GL.TexParameter(TextureTarget.TextureCubeMap,
      TextureParameterName.TextureWrapS, (int)All.ClampToEdge);
    GL.TexParameter(TextureTarget.TextureCubeMap,
      TextureParameterName.TextureWrapT, (int)All.ClampToEdge);
    GL.TexParameter(TextureTarget.TextureCubeMap,
      TextureParameterName.TextureWrapR, (int)All.ClampToEdge);
    
    StbImage.stbi_set_flip_vertically_on_load(0);

    foreach (var (it, i) in new[]
               { "Right", "Left", "Top", "Bottom", "Front", "Back" }.Indexed())
    {
      var res =
        ImageResult.FromStream(
          File.OpenRead($"{path}.{it}.png"),
          ColorComponents.RedGreenBlue);
      
      GL.TexImage2D(
        TexType.TextureCubeMapPositiveX + i,
        0,
        PixelInternalFormat.Rgb8,
        res.Width,
        res.Height,
        0,
        PixFmt.Rgb,
        PixType.UnsignedByte,
        res.Data);
    }
  }

  private static readonly Lazy<(Vao, Buf)> _mesh =
    new(() =>
    {
      var vbo =
        new Buf(BufferTarget.ArrayBuffer)
          .Data(BufUsage.StaticDraw, stackalloc Vec3[]
          {
            // positions
            new Vec3(-1.0f, 1.0f, -1.0f),
            new Vec3(-1.0f, -1.0f, -1.0f),
            new Vec3(1.0f, -1.0f, -1.0f),
            new Vec3(1.0f, -1.0f, -1.0f),
            new Vec3(1.0f, 1.0f, -1.0f),
            new Vec3(-1.0f, 1.0f, -1.0f),

            new Vec3(-1.0f, -1.0f, 1.0f),
            new Vec3(-1.0f, -1.0f, -1.0f),
            new Vec3(-1.0f, 1.0f, -1.0f),
            new Vec3(-1.0f, 1.0f, -1.0f),
            new Vec3(-1.0f, 1.0f, 1.0f),
            new Vec3(-1.0f, -1.0f, 1.0f),

            new Vec3(1.0f, -1.0f, -1.0f),
            new Vec3(1.0f, -1.0f, 1.0f),
            new Vec3(1.0f, 1.0f, 1.0f),
            new Vec3(1.0f, 1.0f, 1.0f),
            new Vec3(1.0f, 1.0f, -1.0f),
            new Vec3(1.0f, -1.0f, -1.0f),

            new Vec3(-1.0f, -1.0f, 1.0f),
            new Vec3(-1.0f, 1.0f, 1.0f),
            new Vec3(1.0f, 1.0f, 1.0f),
            new Vec3(1.0f, 1.0f, 1.0f),
            new Vec3(1.0f, -1.0f, 1.0f),
            new Vec3(-1.0f, -1.0f, 1.0f),

            new Vec3(-1.0f, 1.0f, -1.0f),
            new Vec3(1.0f, 1.0f, -1.0f),
            new Vec3(1.0f, 1.0f, 1.0f),
            new Vec3(1.0f, 1.0f, 1.0f),
            new Vec3(-1.0f, 1.0f, 1.0f),
            new Vec3(-1.0f, 1.0f, -1.0f),

            new Vec3(-1.0f, -1.0f, -1.0f),
            new Vec3(-1.0f, -1.0f, 1.0f),
            new Vec3(1.0f, -1.0f, -1.0f),
            new Vec3(1.0f, -1.0f, -1.0f),
            new Vec3(-1.0f, -1.0f, 1.0f),
            new Vec3(1.0f, -1.0f, 1.0f)
          });

      var vao = new Vao(vbo, null, Vao.Attrib.Float3);

      return (vao, vbo);
    });

  private static readonly Lazy<Shader> _shader =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Skybox.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Skybox.fsh")));

  public void Draw()
  {
    GL.ActiveTexture(TextureUnit.Texture0);
    GL.BindTexture(TextureTarget.TextureCubeMap, Id);
    
    _shader.Get.Bind()
      .Defaults(false)
      .Int("u_tex_skybox", 0);

    var (vao, _) = _mesh.Get;
    vao.Draw(PrimitiveType.Triangles);
  }
}