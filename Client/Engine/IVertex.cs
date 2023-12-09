using Penki.Client.GLU;

namespace Penki.Client.Engine;

public interface IVertex
{
  public static abstract Vao.Attrib[] Attribs { get; }
}