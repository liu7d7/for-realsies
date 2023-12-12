using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Penki.Client.Engine;
using Penki.Client.Game;
using Penki.Client.GLU;

namespace Penki.Client;

public static class Penki
{
  public static readonly BufferPool BufferPool = new BufferPool();
  public static readonly Simulation Simulation = 
    Simulation.Create(
      BufferPool, 
      new DemoNarrowPhaseCallbacks(new SpringSettings(80, 0.8f)), 
      new DemoPoseIntegratorCallbacks(new System.Numerics.Vector3(0, -10, 0)),
      new SolveDescription(8, 4));
  
  public static readonly ThreadDispatcher ThreadDispatcher = 
    new ThreadDispatcher(
      int.Max(
        1, 
        Environment.ProcessorCount > 4 ? 
          Environment.ProcessorCount - 2 : 
          Environment.ProcessorCount - 1));
  
  private static readonly GameWindow _win =
    new GameWindow(
      new GWS
      {
        UpdateFrequency = 60
      },
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
  public static Vec2 SizeF => new(_win.ClientSize.X, _win.ClientSize.Y);
  public static bool Wireframe = false;

  public static CursorState Cursor
  {
    get => _win.CursorState;
    private set => _win.CursorState = value;
  }
  public static bool InFocus => _win.IsFocused;
  public static Vec2 Mouse => _win.MousePosition;

  private static Vec2i FboSize => Size / 4;

  private static readonly Lazy<Fbo> _fbo =
    new(() =>
      new Fbo(
        (FramebufferAttachment.ColorAttachment0, TexConf.Rgba32(FboSize)),
        (FramebufferAttachment.ColorAttachment1, TexConf.R32(FboSize)),
        (FramebufferAttachment.ColorAttachment2, TexConf.Rgba32(FboSize)),
        (FramebufferAttachment.DepthAttachment, TexConf.Depth24(FboSize))));
  
  private static readonly Lazy<Fbo> _tmpFbo =
    new(() =>
      new Fbo((FramebufferAttachment.ColorAttachment0, TexConf.Rgba32(FboSize))));

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

  public static Camera Cam => _player.Cam;

  private static readonly Lazy<World> _world = 
    new(() => new World(_player.Cam).Also(it => it.Add(_player)));

  private static readonly Lazy<Skybox> _sky = new(() =>
    new Skybox(@"Res\Skyboxes\Anime\Anime"));

  private static readonly DebugProc _logDelegate = Log;

  private static readonly RollingAverage _fps = new(300);

  private static bool _step = false;

  private static readonly Lazy<Lightmap> _lightmap =
    new(() => new Lightmap(4096, 4096));

  private static Mat4 Ortho =>
    Mat4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);

  public static Shader Defaults(this Shader sh, RenderSource source, bool translation = true, bool threeD = true)
  {
    sh
      .Mat4("u_proj", source == RenderSource.Lightmap ? _lightmap.Get.Proj : threeD ? Cam.Proj : Ortho)
      .Float3("u_eye", Cam.Eye)
      .Float1("u_time", (float) GLFW.GetTime())
      .Float2("u_one_texel", Vec2.One / SizeF)
      .Float1("u_z_near", source == RenderSource.Lightmap ? Lightmap.Near : Camera.ZNear)
      .Float1("u_z_far", source == RenderSource.Lightmap ? Lightmap.Far : Camera.ZFar)
      .Float3("u_front", Cam.Front)
      .Mat4("u_model", Mat4.Identity)
      .Mat4("u_view", source == RenderSource.Lightmap ? Lightmap.View : translation ? Cam.View : new Matrix4(new Matrix3(Cam.View)))
      .Int("u_light_tex", 7)
      .Mat4("u_light_proj", _lightmap.Get.Proj)
      .Mat4("u_light_view", Lightmap.View)
      .Float1("u_light_z_near", Lightmap.Near)
      .Float1("u_light_z_far", Lightmap.Far);

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
    Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
    
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
  }

  private static void Draw(FrameEventArgs args)
  {
    GL.Enable(EnableCap.DepthTest);
    GL.DepthFunc(DepthFunction.Less);
    GL.Enable(EnableCap.CullFace);
    
    _lightmap.Get.Consume(() => _world.Get.Draw(RenderSource.Lightmap));
    
    GL.Viewport(0, 0, FboSize.X, FboSize.Y);
    
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
    _lightmap.Get.Fbo.BindTex(FramebufferAttachment.DepthAttachment, 7);
    
    _world.Get.Draw(RenderSource.World);
    
    GL.DepthFunc(DepthFunction.Lequal);
    
    _sky.Get.Draw();
    
    GL.Disable(EnableCap.DepthTest);
    GL.Disable(EnableCap.CullFace);

    _tmpFbo.Get.Bind();
    fbo.BindTex(FramebufferAttachment.ColorAttachment0, 0);
    _dither.Get.Bind()
      .Int("u_tex_col", 0)
      .Int("u_pal_size", DreamyHaze.Colors.Length)
      .Float3V("u_pal", DreamyHaze.Colors);
    PostProcess.DrawFullscreenQuad();
    
    Fbo.Bind0();
    
    GL.BlitNamedFramebuffer(
      _tmpFbo.Get.Id, 0,
      0, 0, FboSize.X, FboSize.Y,
      0, 0, Size.X, Size.Y,
      ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
    
    GL.Viewport(0, 0, Size.X, Size.Y);
    _lightmap.Get.Fbo.BindTex(FboComp.DepthAttachment, 0);
    PostProcess.Blit((Size.X - 400, Size.Y - 400), Size);
    
    GL.Enable(EnableCap.Blend);
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    _fps.Add(args.Time);
    Font.Draw($"pos: {_player.Pos.X:.00}, {_player.Pos.Y:.00}, {_player.Pos.Z:.00}", 10, 10, Vec3.One, true);
    Font.Draw($"cpos: {_player.Pos.ToChunk().X}, {_player.Pos.ToChunk().Y}", 10, 50, Vec3.One, true);
    Font.Draw($"fps: {1.0 / _fps.Average:.00}", 10, 90, Vec3.One, true);
    Font.Draw($"mem: {GC.GetTotalMemory(false) / 1024 / 1024}M", 10, 130, Vec3.One, true);

    _win.SwapBuffers();
  }

  private static void Resize(ResizeEventArgs args)
  {
    ((Fbo)_fbo).Resize(
      (FboComp.ColorAttachment0, FboSize),
      (FboComp.ColorAttachment1, FboSize),
      (FboComp.ColorAttachment2, FboSize),
      (FboComp.DepthAttachment, FboSize));
    
    ((Fbo)_tmpFbo).Resize(
      (FboComp.ColorAttachment0, FboSize));

    GL.Viewport(0, 0, args.Size.X, args.Size.Y);
  }

  private const float TimeStep = 1f / 60f;
  
  private static void Tick(FrameEventArgs args)
  {
    if (_step)
    {
      Simulation.Timestep(TimeStep, ThreadDispatcher);
    }
    
    Cam.Tick();
    _world.Get.Tick((float) args.Time);
  }

  private static void MouseMove(MouseMoveEventArgs args)
  {
    Cam.Look();
  }

  private static void MouseDown(MouseButtonEventArgs args)
  {
    Cursor = CursorState.Grabbed;
    _player.MouseDown(args);
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
      case Keys.I:
        Wireframe = !Wireframe;
        break;
      case Keys.O:
        _step = !_step;
        break;
    }
    
    _player.KeyDown(args);
  }

}