global using GWS = OpenTK.Windowing.Desktop.GameWindowSettings;
global using NWS = OpenTK.Windowing.Desktop.NativeWindowSettings;
global using FboComp = OpenTK.Graphics.OpenGL4.FramebufferAttachment;
global using SzIntFmt = OpenTK.Graphics.OpenGL4.SizedInternalFormat;
global using PixIntFmt = OpenTK.Graphics.OpenGL4.PixelInternalFormat;
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
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics;
using OpenTK.Mathematics;
using Vector3 = System.Numerics.Vector3;

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

public class Lazy<T>(Func<T> supplier)
{
  private bool _initialized;
  private T? _value;

  public T Get
  {
    get
    {
      if (!_initialized)
      {
        _value = supplier();
        _initialized = true;
      }

      if (_value == null)
      {
        throw new NullReferenceException("Failed to initialize lazy value.");
      }

      return _value;
    }
  }

  public static implicit operator T(Lazy<T> lazy) => lazy.Get;
}

public class Memo<TValue, TArg>(Func<TArg, TValue> consumer)
  where TArg : notnull
{
  private readonly Dictionary<TArg, TValue> _memo = new();

  public TValue this[TArg arg]
  {
    get
    {
      ref var value = ref CollectionsMarshal.GetValueRefOrNullRef(_memo, arg);
      if (!Unsafe.IsNullRef(ref value)) return value;

      var val = consumer(arg);
      _memo.Add(arg, val);
      return val;
    }
  }
}

public static class Extensions
{
  public delegate void Consumer<in T>(T me);

  public static void DoNothing<T>(this T me)
  {
    
  }

  public static IEnumerable<(T, int)> Indexed<T>(this IEnumerable<T> orig)
  {
    return orig.Select((it, i) => (it, i));
  }

  public static void SetPos(this BodyHandle handle, Simulation sim, Vec3 pos)
  {
    var desc = sim.Bodies.GetDescription(handle);
    sim.Bodies.ApplyDescription(handle, desc with
    {
      Pose = desc.Pose with { Position = pos.ToNumerics() }
    });
  }
  
  public static Vec3 GetPos(this BodyHandle handle, Simulation sim)
  {
    return sim.Bodies.GetDescription(handle).Pose.Position.ToTk();
  }
  
  public static void SetVel(this BodyHandle handle, Simulation sim, Vec3 vel)
  {
    var desc = sim.Bodies.GetDescription(handle);
    sim.Bodies.ApplyDescription(handle, desc with
    {
      Velocity = desc.Velocity with { Linear = vel.ToNumerics() }
    });
  }
  
  public static Vec3 GetVel(this BodyHandle handle, Simulation sim)
  {
    return sim.Bodies.GetDescription(handle).Velocity.Linear.ToTk();
  }

  public static System.Numerics.Quaternion ToNumerics(this Quaternion quat)
  {
    return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
  }
  
  public static Quaternion ToTk(this System.Numerics.Quaternion quat)
  {
    return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
  }
  
  public static void SetOrient(this BodyHandle handle, Simulation sim, Quaternion orient)
  {
    var desc = sim.Bodies.GetDescription(handle);
    sim.Bodies.ApplyDescription(handle, desc with
    {
      Pose = desc.Pose with { Orientation = orient.ToNumerics() }
    });
  }
  
  public static Quaternion GetOrient(this BodyHandle handle, Simulation sim)
  {
    return sim.Bodies.GetDescription(handle).Pose.Orientation.ToTk();
  }

  public static void Return<T>(this T[] arr)
  {
    ArrayPool<T>.Shared.Return(arr);
  }

  public static Vector3 ToNumerics(this Vec3 vec)
  {
    return new Vector3(vec.X, vec.Y, vec.Z);
  }

  public static Vec3 ToTk(this Vector3 vec)
  {
    return new Vec3(vec.X, vec.Y, vec.Z);
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

  public static float AngleLerp(this float start, float end, float delta)
  {
    float shortestAngle = ((end - start) % 360 + 540) % 360 - 180;
    return start + shortestAngle * delta;
  }

  public static float Cos(this float rad)
  {
    return MathF.Cos(rad);
  }

  public static float Sin(this float rad)
  {
    return MathF.Sin(rad);
  }

  public static Vec3 NormalizedSafe(this Vec3 vec)
  {
    return vec.LengthSquared > 0.00001 ? vec.Normalized() : vec;
  }

  public static float Lerp(this float start, float end, float delta)
  {
    return MathHelper.Lerp(start, end, delta);
  }


  public static Span<T> AsSpan<T>(this List<T> list)
  {
    return CollectionsMarshal.AsSpan(list);
  }

  public static float Dist(this Vec3 lhs, Vec3 rhs)
  {
    return (lhs - rhs).Length;
  }

  public static float Dist2(this Vec3 lhs, Vec3 rhs)
  {
    return (lhs - rhs).LengthSquared;
  }

  public static void Scale(this ref Mat4 mat, Vec3 scale)
  {
    var trans = mat.ExtractTranslation();
    mat = mat.ClearTranslation();
    mat *= Mat4.CreateScale(scale);
    mat *= Mat4.CreateTranslation(trans);
  }

  public static void Rotate(this ref Mat4 mat, Vec3 axis, float deg)
  {
    var trans = mat.ExtractTranslation();
    mat = mat.ClearTranslation();
    mat *= Mat4.CreateFromAxisAngle(axis.Normalized(), deg.Rad());
    mat *= Mat4.CreateTranslation(trans);
  }
  
  public static void Rotate(this ref Mat4 mat, Quaternion rot)
  {
    var trans = mat.ExtractTranslation();
    mat = mat.ClearTranslation();
    mat *= Mat4.CreateFromQuaternion(rot);
    mat *= Mat4.CreateTranslation(trans);
  }

  public static void Translate(this ref Mat4 mat, Vec3 trans)
  {
    var (x, y, z) = trans;
    mat *= Mat4.CreateTranslation(Vec4
      .Transform(new Vec4(x, y, z, 1.0f), mat.ExtractRotation()).Xyz);
  }

  public static Mat4 ChangeAxis(this Mat4 mat, Vec3 axis, int axNum)
  {
    var t = axis.Normalized();
    int min = 0;
    if (Math.Abs(t[min]) > Math.Abs(t[1])) min = 1;
    if (Math.Abs(t[min]) > Math.Abs(t[2])) min = 2;

    var m = Vec3.Zero;
    m[min] = 1;

    var f = Vec3.Cross(t, m).Normalized();
    var s = Vec3.Cross(t, f).Normalized();
    var (x, y, z) = mat.ExtractTranslation();

    var changed = axNum switch
    {
      0 => new Mat4(
        t.X, t.Y, t.Z, 0,
        f.X, f.Y, f.Z, 0,
        s.X, s.Y, s.Z, 0,
        x, y, z, 1),
      1 => new Mat4(
        s.X, t.Y, s.Z, 0,
        t.X, t.Y, t.Z, 0,
        f.X, f.Y, f.Z, 0,
        x, y, z, 1),
      2 => new Mat4(
        f.X, f.Y, f.Z, 0,
        s.X, s.Y, s.Z, 0,
        t.X, t.Y, t.Z, 0,
        x, y, z, 1),
      _ => throw new Exception("axis out of bounds!")
    };

    return changed;
  }
}

public class RollingAverage
{
  private readonly int _size;
  private readonly Queue<double> _values = new();
  private double _sum;

  public RollingAverage(int size)
  {
    _size = size;
  }

  public double Average => _sum / _values.Count;

  public void Add(double value)
  {
    _sum += value;
    _values.Enqueue(value);
    if (_values.Count <= _size) return;
    _sum -= _values.Dequeue();
  }
}

public class Maybe<T>
{
  public readonly bool HasValue;
  private readonly T? _value;

  public Maybe(T value)
  {
    _value = value;
    HasValue = true;
  }

  public Maybe()
  {
    _value = default;
    HasValue = false;
  }

  public T Value => !HasValue ? throw new NullReferenceException("Empty optional unwrapped!") : _value;
}