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
  private Buf[] _vbos;
  private int _attribs = 0;
  
  public Vao(Buf vbo, Buf? ibo, params Attrib[] attribs)
  {
    (_vbos, _ibo) = (Array.Empty<Buf>(), ibo);
    
    GL.CreateVertexArrays(1, out Id);

    AddVbo(vbo, 0, attribs);
    _size = () => vbo.Size;

    if (ibo == null) return;
    
    GL.VertexArrayElementBuffer(Id, ibo.Id);
    _size = () => ibo.Size;
  }

  public void AddVbo(Buf vbo, int divisor, params Attrib[] attribs)
  {
    _vbos = _vbos.Concat(new[] { vbo }).ToArray();
    int stride = attribs.Sum(AttribSizeInBytes);
    int bindingindex = _vbos.Length - 1;
    GL.VertexArrayVertexBuffer(Id, bindingindex, vbo.Id, 0, stride);
    
    int off = 0;
    foreach (var (it, j) in attribs.Indexed())
    {
      var i = _attribs + j;
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
      
      GL.VertexArrayAttribBinding(Id, i, bindingindex);

      if (divisor != 0)
      {
        GL.VertexArrayBindingDivisor(Id, bindingindex, divisor);
      }
      
      off += AttribSizeInBytes(it);
    }

    _attribs += attribs.Length;
  }

  private void Bind()
  {
    GL.BindVertexArray(Id);
  }

  public void Draw(PrimType type)
  {
    Bind();
    
    foreach (var it in _vbos)
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

    Penki.Tris += _size() / 3;
  }
  
  public void DrawInstanced(PrimType type, int instanceCount)
  {
    Debug.Assert(_ibo == null);
    
    Bind();
    
    foreach (var it in _vbos)
    {
      it.Bind();
    }

    GL.DrawArraysInstanced(type, 0, _size(), instanceCount);
    
    Penki.Tris += _size() / 3 * instanceCount;
  }

  public static int AttribSizeInBytes(Attrib it)
  {
    return AttribSize(it) * sizeof(float);
  }
  
  public static int AttribSize(Attrib it)
  {
    if (it == Attrib.Mat4)
    {
      return 16;
    }
    
    return (int)it % 4 + 1;
  }

  public static VertexAttribType AttribType(Attrib it)
  {
    if (it == Attrib.Mat4)
    {
      return VertexAttribType.Float;
    }
    
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
    Int4,
    Mat4
  }
}