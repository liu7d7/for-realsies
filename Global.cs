global using GWS = OpenTK.Windowing.Desktop.GameWindowSettings;
global using NWS = OpenTK.Windowing.Desktop.NativeWindowSettings;
global using FboComp = OpenTK.Graphics.OpenGL4.FramebufferAttachment;
global using PixIntFmt = OpenTK.Graphics.OpenGL4.SizedInternalFormat;
global using PixFmt = OpenTK.Graphics.OpenGL4.PixelFormat;
global using PixType = OpenTK.Graphics.OpenGL4.PixelType;
global using TexType = OpenTK.Graphics.OpenGL4.TextureTarget;
global using TexParam = OpenTK.Graphics.OpenGL4.TextureParameterName;
global using MinFilter = OpenTK.Graphics.OpenGL4.TextureMinFilter;
global using MagFilter = OpenTK.Graphics.OpenGL4.TextureMagFilter;
global using BufType = OpenTK.Graphics.OpenGL4.BufferTarget;
global using BufUsage = OpenTK.Graphics.OpenGL4.BufferUsageHint;
global using BufMask = OpenTK.Graphics.OpenGL4.ClearBufferMask;
global using BlitFilter = OpenTK.Graphics.OpenGL4.BlitFramebufferFilter;
global using PrimType = OpenTK.Graphics.OpenGL4.PrimitiveType;
global using TexUnit = OpenTK.Graphics.OpenGL4.TextureUnit;
global using Vec2 = OpenTK.Mathematics.Vector2;
global using Vec2i = OpenTK.Mathematics.Vector2i;
global using Vec3 = OpenTK.Mathematics.Vector3;
global using Vec4 = OpenTK.Mathematics.Vector4;
global using Mat4 = OpenTK.Mathematics.Matrix4;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Penki;

public class Pair<TA, TB>
{
  public TA A;
  public TB B;

  public Pair(TA a, TB b)
  {
    A = a;
    B = b;
  }

  public void Deconstruct(out TA a, out TB b)
  {
    a = A;
    b = B;
  }
}

public class Triple<TA, TB, TC>
{
  public TA A;
  public TB B;
  public TC C;

  public Triple(TA a, TB b, TC c)
  {
    A = a;
    B = b;
    C = c;
  }

  public void Deconstruct(out TA a, out TB b, out TC c)
  {
    a = A;
    b = B;
    c = C;
  }
}

public class Lazy<T>
{
  private readonly Func<T> _supplier;
  private bool _initialized;
  private T? _value;

  public Lazy(Func<T> supplier)
  {
    _supplier = supplier;
  }

  public T It
  {
    get
    {
      if (!_initialized)
      {
        _value = _supplier();
        _initialized = true;
      }

      if (_value == null)
      {
        throw new NullReferenceException("Failed to initialize lazy value.");
      }

      return _value;
    }
  }

  public static implicit operator T(Lazy<T> lazy) => lazy.It;
}

public static class Extensions
{
  public delegate void Consumer<in T>(T me);
  
  public static IEnumerable<(T, int)> Indexed<T>(this IEnumerable<T> orig)
  {
    return orig.Select((it, i) => (it, i));
  }

  public static Vec2 To2(this Vec3 vec)
  {
    return new Vec2(vec.X, vec.Y);
  }

  public static Vec3 ToVec3(this float[] vec)
  {
    return new Vec3(vec[0], vec[1], vec[2]);
  }

  public static T Also<T>(this T me, Consumer<T> f)
  {
    f(me);
    return me;
  }

  public static TR Let<T, TR>(this T me, Func<T, TR> f)
  {
    return f(me);
  }

  public static float Rad(this float deg)
  {
    return MathHelper.DegreesToRadians(deg);
  }
  
  public static float Deg(this float rad)
  {
    return MathHelper.RadiansToDegrees(rad);
  }

  public static Span<T> AsSpan<T>(this List<T> list)
  {
    return CollectionsMarshal.AsSpan(list);
  }

}