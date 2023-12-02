using OpenTK.Graphics.OpenGL4;
using GL = OpenTK.Graphics.OpenGL4.GL;
using VertexAttribType = OpenTK.Graphics.OpenGL4.VertexAttribType;

namespace Penki.Client.GLU;

public class Vao
{
  public readonly int Id;
  private Func<int> _size;
  private bool _hasIbo;
  
  public Vao(Buf vbo, Buf? ibo, params Attrib[] attribs)
  {
    GL.CreateVertexArrays(1, out Id);
    
    int stride = attribs.Sum(AttribSizeInBytes);
    GL.VertexArrayVertexBuffer(Id, 0, vbo.Id, IntPtr.Zero, stride);
    _size = () => vbo.Size;
    _hasIbo = false;
    
    if (ibo != null)
    {
      GL.VertexArrayElementBuffer(Id, ibo.Id);
      _size = () => ibo.Size;
      _hasIbo = true;
    }

    int off = 0;
    foreach (var (it, i) in attribs.Indexed())
    {
      GL.EnableVertexArrayAttrib(Id, i);
      GL.VertexArrayAttribFormat(Id, i, AttribSize(it), AttribType(it), false, off);
      GL.VertexArrayAttribBinding(Id, i, 0);
      
      off += AttribSizeInBytes(it);
    }
  }

  public Vao Bind()
  {
    GL.BindVertexArray(Id);
    return this;
  }

  public void Draw(PrimType type)
  {
    if (_hasIbo)
    {
      GL.DrawElements(type, _size(), DrawElementsType.UnsignedInt, 0);
    }
    else
    {
      GL.DrawArrays(type, 0, _size());
    }
  }

  public static int AttribSizeInBytes(Attrib it)
  {
    return AttribSize(it) * 4;
  }
  
  public static int AttribSize(Attrib it)
  {
    return (int)it % 4 + 1;
  }

  public static VertexAttribType AttribType(Attrib it)
  {
    return (int)it >= 4 ? VertexAttribType.Int : VertexAttribType.Float;
  } 
  
  public enum Attrib
  {
    Float1,
    Float2,
    Float3,
    Float4,
    Int1,
    Int2,
    Int3,
    Int4
  }
}