using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Penki.Client.Engine;
using Penki.Client.Game;
using Penki.Client.GLU;
using Penki.Game;

namespace Penki.Client;

public static class Penki
{
  private static readonly GameWindow _win =
    new GameWindow(
      GWS.Default,
      new NWS
      {
        Size = new Vector2i(1920, 1200),
        Flags = ContextFlags.Debug,
        API = ContextAPI.OpenGL,
        APIVersion = new Version(4, 6),
        Profile = ContextProfile.Core,
        Title = "ﾍﾟﾝｷ"
      });

  public static Vector2i Size => _win.ClientSize;
  public static Vector2 SizeF => new(_win.ClientSize.X, _win.ClientSize.Y);

  public static CursorState Cursor
  {
    get => _win.CursorState;
    private set => _win.CursorState = value;
  }
  public static bool InFocus => _win.IsFocused;
  public static Vector2 Mouse => _win.MousePosition;

  private static readonly Vec2i _fboSize = Size / 4;

  private static readonly Lazy<Fbo> _fbo =
    new(() =>
      new Fbo(
        (FramebufferAttachment.ColorAttachment0, TexConf.Rgba32(_fboSize)),
        (FramebufferAttachment.ColorAttachment1, TexConf.R32(_fboSize)),
        (FramebufferAttachment.ColorAttachment2, TexConf.Rgba32(_fboSize)),
        (FramebufferAttachment.DepthAttachment, TexConf.Depth24(_fboSize))));
  
  private static readonly Lazy<Fbo> _tmpFbo =
    new(() =>
      new Fbo((FramebufferAttachment.ColorAttachment0, TexConf.Rgba32(_fboSize))));

  private static readonly Lazy<Shader> _outline =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Postprocess.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Outline.fsh")));
  
  private static readonly Lazy<Shader> _dither =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Postprocess.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Dither.fsh")));

  private static readonly Player _player = new Player();

  private static Camera Cam => _player.Cam;

  private static readonly World _world = 
    new World(_player.Cam)
      .Also(it => it.Add(_player));

  private static readonly Lazy<Skybox> _sky = new(() =>
    new Skybox(@"Res\Skyboxes\Anime\Anime"));

  private static readonly DebugProc _logDelegate = Log;

  public static Shader Defaults(this Shader sh, bool translation = true)
  {
    sh
      .Mat4("u_proj", Cam.Proj)
      .Float3("u_eye", Cam.Eye)
      .Float1("u_time", (float) GLFW.GetTime())
      .Float2("u_one_texel", Vec2.One / SizeF)
      .Float1("u_z_near", Camera.ZNear)
      .Float1("u_z_far", Camera.ZFar)
      .Float3("u_front", Cam.Front)
      .Mat4("u_model", Mat4.Identity)
      .Mat4("u_view", translation ? Cam.View : new Matrix4(new Matrix3(Cam.View)));

    return sh;
  }

  private static void Log(
    DebugSource source,
    DebugType type,
    int id,
    DebugSeverity severity,
    int length,
    IntPtr pMessage,
    IntPtr pUserParam)
  {
    string message = Marshal.PtrToStringAnsi(pMessage, length);

    Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source,
      type, id, message);

    if (type == DebugType.DebugTypeError)
    {
      throw new Exception(message);
    }
  }

  public static void Run()
  {
    _win.RenderFrame += Draw;
    _win.UpdateFrame += Tick;
    _win.Load += Init;
    _win.Resize += Resize;
    _win.MouseMove += MouseMove;
    _win.MouseDown += MouseDown;
    _win.KeyDown += KeyDown;
    _win.Run();
    _win.Dispose();
  }

  public static bool IsDown(Keys key)
  {
    return _win.IsKeyDown(key);
  }

  private static void Init()
  {
    GL.Viewport(0, 0, Size.X, Size.Y);

    GL.DebugMessageCallback(_logDelegate, IntPtr.Zero);
    GL.Enable(EnableCap.DebugOutput);

    Cursor = CursorState.Grabbed;

    var tex = new Tex(@"Res\Skyboxes\Noon\Noon.Left.png");
    tex.Bind(TextureUnit.Texture5);
  }

  private static void Draw(FrameEventArgs args)
  {
    GL.Viewport(0, 0, _fboSize.X, _fboSize.Y);
    
    var fbo = (Fbo)_fbo;
    fbo.Bind()
      .DrawBuffers(
        DrawBuffersEnum.ColorAttachment0,
        DrawBuffersEnum.ColorAttachment1,
        DrawBuffersEnum.ColorAttachment2)
      .Clear(ClearBuffer.Color, 0, stackalloc float[] {0, 0, 0, 0})
      .Clear(ClearBuffer.Color, 1, stackalloc uint[] {0, 0, 0, 0})
      .Clear(ClearBuffer.Color, 2, stackalloc float[] {0, 0, 0, 0})
      .Clear(ClearBuffer.Depth, 0, stackalloc float[] {1});
    
    GL.Enable(EnableCap.DepthTest);
    GL.DepthFunc(DepthFunction.Less);
    
    _world.Draw();
    
    GL.DepthFunc(DepthFunction.Lequal);
    
    _sky.Get.Draw();
    
    GL.Disable(EnableCap.DepthTest);

    _tmpFbo.Get.Bind();
    fbo.BindTex(FramebufferAttachment.ColorAttachment0, 0);
    _dither.Get.Bind()
      .Defaults()
      .Int("u_tex_col", 0)
      .Int("u_pal_size", DreamyHaze.Colors.Length)
      .Float3V("u_pal", DreamyHaze.Colors);
    PostProcess.DrawFullscreenQuad();
    
    GL.BlitNamedFramebuffer(
      _tmpFbo.Get.Id, 0,
      0, 0, _fboSize.X, _fboSize.Y,
      0, 0, Size.X, Size.Y,
      ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

    _win.SwapBuffers();
  }

  private static void Resize(ResizeEventArgs args)
  {
    ((Fbo)_fbo).Resize(
      (FboComp.ColorAttachment0, _fboSize),
      (FboComp.ColorAttachment1, _fboSize),
      (FboComp.ColorAttachment2, _fboSize),
      (FboComp.DepthAttachment, _fboSize));
    
    ((Fbo)_tmpFbo).Resize(
      (FboComp.ColorAttachment0, _fboSize));

    GL.Viewport(0, 0, args.Size.X, args.Size.Y);
  }

  private static void Tick(FrameEventArgs args)
  {
    Cam.Tick();
  }

  private static void MouseMove(MouseMoveEventArgs args)
  {
    Cam.Look();
  }

  private static void MouseDown(MouseButtonEventArgs args)
  {
    Cursor = CursorState.Grabbed;
  }

  private static void KeyDown(KeyboardKeyEventArgs args)
  {
    switch (args.Key)
    {
      case Keys.Escape:
        Cursor = CursorState.Normal;
        break;
      case Keys.R when args.Modifiers.HasFlag(KeyModifiers.Control):
        Reloader.Load();
        break;
    }
  }

}