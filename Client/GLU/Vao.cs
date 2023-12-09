using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using GL = OpenTK.Graphics.OpenGL4.GL;
using VertexAttribType = OpenTK.Graphics.OpenGL4.VertexAttribType;

namespace Penki.Client.GLU;

public class Vao
{
  public readonly int Id;
  private readonly Func<int> _size;
  private readonly Buf? _ibo;
  private readonly Buf[] _vbo;
  
  public Vao(Buf vbo, Buf? ibo, params Attrib[] attribs)
  {
    (_vbo, _ibo) = (new[] { vbo }, ibo);
    
    GL.CreateVertexArrays(1, out Id);
    
    int stride = attribs.Sum(AttribSizeInBytes);
    GL.VertexArrayVertexBuffer(Id, 0, vbo.Id, IntPtr.Zero, stride);
    _size = () => vbo.Size;
    
    if (ibo != null)
    {
      GL.VertexArrayElementBuffer(Id, ibo.Id);
      _size = () => ibo.Size;
    }

    int off = 0;
    foreach (var (it, i) in attribs.Indexed())
    {
      GL.EnableVertexArrayAttrib(Id, i);
      if (AttribType(it) == VertexAttribType.Float)
      {
        GL.VertexArrayAttribFormat(Id, i, AttribSize(it), AttribType(it), false,
          off);
      }
      else
      {
        GL.VertexArrayAttribIFormat(Id, i, AttribSize(it), AttribType(it), off);
      }
      GL.VertexArrayAttribBinding(Id, i, 0);
      
      off += AttribSizeInBytes(it);
    }
  }
  
  public Vao(Buf[] vbo, Func<int> size, params (Attrib, int, int)[] attribs)
  {
    (_vbo, _ibo, _size) = (vbo, null, size);
    
    GL.CreateVertexArrays(1, out Id);
    
    int stride = attribs.Select(it => it.Item1).Sum(AttribSizeInBytes);
    foreach (var (it, i) in vbo.Indexed())
    {
      GL.VertexArrayVertexBuffer(Id, i, it.Id, IntPtr.Zero, stride);
    }

    int off = 0;
    foreach (var ((it, binding, div), i) in attribs.Indexed())
    {
      GL.EnableVertexArrayAttrib(Id, i);
      if (AttribType(it) == VertexAttribType.Float)
      {
        GL.VertexArrayAttribFormat(Id, i, AttribSize(it), AttribType(it), false,
          off);
      }
      else
      {
        GL.VertexArrayAttribIFormat(Id, i, AttribSize(it), AttribType(it), off);
      }

      if (div != 0)
      {
        GL.VertexArrayBindingDivisor(Id, binding, div);
      }
      
      GL.VertexArrayAttribBinding(Id, i, binding);
      
      off += AttribSizeInBytes(it);
    }
  }

  private void Bind()
  {
    GL.BindVertexArray(Id);
  }

  public void Draw(PrimType type)
  {
    Bind();
    
    foreach (var it in _vbo)
    {
      it.Bind();
    }
    
    if (_ibo != null)
    {
      _ibo.Bind();
      GL.DrawElements(type, _size(), DrawElementsType.UnsignedInt, 0);
    }
    else
    {
      GL.DrawArrays(type, 0, _size());
    }
  }
  
  public void DrawInstanced(PrimType type, int instanceCount)
  {
    Debug.Assert(_ibo == null);
    
    Bind();
    
    foreach (var it in _vbo)
    {
      it.Bind();
    }

    GL.DrawArraysInstanced(type, 0, _size(), instanceCount);
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