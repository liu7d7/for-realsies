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
        Profile = ContextProfile.Core
      });

  public static Vector2i Size => _win.Size;
  public static Vector2 SizeF => new(_win.Size.X, _win.Size.Y);

  public static CursorState Cursor
  {
    get => _win.CursorState;
    private set => _win.CursorState = value;
  }
  public static bool InFocus => _win.IsFocused;
  public static Vector2 Mouse => _win.MousePosition;

  private static readonly Lazy<Fbo> _fbo =
    new(() =>
      new Fbo(
        (FramebufferAttachment.ColorAttachment0,
          TexConf.Rgba32(Size)),
        (FramebufferAttachment.DepthAttachment,
          TexConf.Depth24(Size))));

  private static readonly Lazy<Model> _mod =
    new(() => new Model(@"Res\Models\Monkey.obj"));

  private static readonly Camera _cam = new Camera();

  private static readonly World _world = new World(_cam);

  private static readonly DebugProc _logDelegate = Log;

  public static Shader Defaults(this Shader sh)
  {
    sh
      .Mat4("u_proj", _cam.Proj)
      .Float3("u_eye", _cam.Eye)
      .Float1("u_time", (float) GLFW.GetTime())
      .Mat4("u_view", _cam.View);

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
    
    _world.Add(new Player());
  }

  private static void Draw(FrameEventArgs args)
  {
    ((Fbo)_fbo).Bind()
      .Clear();
    
    GL.Enable(EnableCap.DepthTest);
    GL.DepthFunc(DepthFunction.Lequal);

    ((Model)_mod).Draw();
    _world.Draw();

    GL.BlitNamedFramebuffer(
      ((Fbo)_fbo).Id, 0,
      0, 0, Size.X, Size.Y,
      0, 0, Size.X, Size.Y,
      BufMask.ColorBufferBit, BlitFilter.Linear);

    _win.SwapBuffers();
  }

  private static void Resize(ResizeEventArgs args)
  {
    ((Fbo)_fbo).Resize(
      (FboComp.ColorAttachment0, args.Size),
      (FboComp.DepthAttachment, args.Size));

    GL.Viewport(0, 0, args.Size.X, args.Size.Y);
  }

  private static void Tick(FrameEventArgs args)
  {
    _cam.Tick();
  }

  private static void MouseMove(MouseMoveEventArgs args)
  {
    _cam.Look();
  }

  private static void MouseDown(MouseButtonEventArgs args)
  {
    Cursor = CursorState.Grabbed;
  }

  private static void KeyDown(KeyboardKeyEventArgs args)
  {
    if (args.Key == Keys.Escape)
    {
      Cursor = CursorState.Normal;
    }

    if (args.Key == Keys.R && args.Modifiers.HasFlag(KeyModifiers.Control))
    {
      Reloader.Load();
    }
  }

}