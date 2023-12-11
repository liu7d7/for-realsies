using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Penki.Client.GLU;

public class Buf
{
  public readonly int Id;
  public readonly BufType Type;
  public int Size { get; private set; }

  public Buf(BufType type)
  {
    Type = type;
    GL.CreateBuffers(1, out Id);
  }

  public Buf Data<T>(BufUsage usage, Span<T> data)
    where T : struct
  {
    Size = data.Length;
    var bits = data.Length * Marshal.SizeOf<T>();
    GL.NamedBufferData(Id, bits, ref data[0], usage);

    return this;
  }
  
  public Buf Data<T>(BufUsage usage, Span<T> data, int length)
    where T : struct
  {
    Size = length;
    var bits = length * Marshal.SizeOf<T>();
    GL.NamedBufferData(Id, bits, ref data[0], usage);

    return this;
  }
  
  public Buf Data<T>(BufUsage usage, T[] data)
    where T : struct
  {
    Size = data.Length;
    var bytes = data.Length * Marshal.SizeOf<T>();
    GL.NamedBufferData(Id, bytes, ref data[0], usage);

    return this;
  }
  
  public Buf Data<T>(BufUsage usage, int size, ref T data)
    where T : struct
  {
    Size = size;
    GL.NamedBufferData(Id, size * Marshal.SizeOf<T>(), ref data, usage);
    
    return this;
  }

  public void Bind()
  {
    GL.BindBuffer(Type, Id);
  }
}