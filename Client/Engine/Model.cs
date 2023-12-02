using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL4;
using Penki.Client.GLU;

namespace Penki.Client.Engine;

public class Material
{
  public required Vec3 Diff;
  public required Vec3 Ambi;
  public required Vec3 Spec;
  public required int Normals;
}

public class ObjObj
{
  public required List<ObjVtx> Mesh;
  public required Material Mat;
  public required string Name;
  public required Vao Vao;
  public required Buf Vbo;
}

public class Model
{
  private readonly List<ObjObj> _objs = new();
  private readonly List<Tex> _texes;

  private static readonly Lazy<Shader> _sh =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, "Res/Shaders/Model.vsh"),
        (ShaderType.FragmentShader, "Res/Shaders/Model.fsh")));

  public Model(string path)
  {
    var txt = File.ReadAllLines(path);
    var verts = new List<Vec3>();
    var uvs = new List<Vec2>();
    var norms = new List<Vec3>();
    var vertOff = 0;
    var uvOff = 0;
    var normOff = 0;
    var finalVerts = new List<ObjVtx>();
    var name = null as string;
    var (mats, texes) = ReadMats(path[..path.LastIndexOf('\\')], path[(path.LastIndexOf('\\') + 1)..path.LastIndexOf('.')]);
    _texes = texes;
    
    foreach (var line in txt)
    {
      if (line.StartsWith("#") || 
          line.StartsWith("mtllib") || 
          line.StartsWith("usemtl") ||
          line.StartsWith("s "))
      {
        continue;
      }

      if (line.StartsWith("o "))
      {
        if (name != null)
        {
          NewObj();
        }
        
        name = line[2..];
        vertOff += verts.Count;
        uvOff += uvs.Count;
        normOff += norms.Count;

        continue;
      }

      if (line.StartsWith("v "))
      {
        var xyz = 
          line[2..]
            .Split(" ")
            .Select(it => 
              float.Parse(it.AsSpan(), CultureInfo.InvariantCulture))
            .ToArray();
        
        verts.Add(new Vec3(xyz[0], xyz[1], xyz[2]));

        continue;
      }

      if (line.StartsWith("vt "))
      {
        var xyz = 
          line[3..]
            .Split(" ")
            .Select(it => 
              float.Parse(it.AsSpan(), CultureInfo.InvariantCulture))
            .ToArray();
        
        uvs.Add(new Vec2(xyz[0], xyz[1]));

        continue;
      }
      
      if (line.StartsWith("vn "))
      {
        var xyz = 
          line[3..]
            .Split(" ")
            .Select(it => 
              float.Parse(it.AsSpan(), CultureInfo.InvariantCulture))
            .ToArray();
        
        norms.Add(new Vec3(xyz[0], xyz[1], xyz[2]));

        continue;
      }

      if (line.StartsWith("f "))
      {
        var vertsOfFace =
          line[2..]
            .Split(" ")
            .Select(it =>
              it.Split("/").Select(idx => int.Parse(idx.AsSpan())).ToArray())
            .Select(it => 
              new ObjVtx(
                verts[it[0] - vertOff - 1], 
                uvs[it[1] - uvOff - 1], 
                norms[it[2] - normOff - 1]))
            .ToArray();
        
        finalVerts.AddRange(vertsOfFace);

        continue;
      }

      throw new 
        InvalidDataException($"If this is reached then that is bad.\n{line}");
    }
    
    NewObj();
    return;

    void NewObj()
    {
      if (name == null)
        throw new InvalidDataException("Expected Name, got null.");
      
      var vbo = 
        new Buf(BufType.ArrayBuffer)
          .Data(
            BufUsage.DynamicDraw, 
            finalVerts.AsSpan());
      
      var vao = new Vao(vbo, null, ObjVtx.Attribs);

      _objs.Add(new ObjObj
      {
        Name = name,
        Mat = mats[name],
        Mesh = finalVerts,
        Vbo = vbo,
        Vao = vao
      });
    }
  }

  public void Draw(Camera cam)
  {
    foreach (var obj in _objs)
    {
      if (obj.Mat.Normals != -1)
      {
        _texes[obj.Mat.Normals].Bind(TexUnit.Texture0);
      }
      
      var sh = (Shader)_sh;
      sh.Bind()
        .Defaults()
        .Float3("u_eye", cam.Eye)
        .Mat(obj.Mat);
      obj.Vbo.Bind();
      obj.Vao.Bind().Draw(PrimType.Triangles);
    }
  }

  private static (Dictionary<string, Material>, List<Tex>) ReadMats(string dir, string fileName)
  {
    var texes = new List<Tex>();
    var mats = new Dictionary<string, Material>();
    var json = JObject.Parse(File.ReadAllText(dir + $@"\{fileName}.mtl.json"))
      ?? throw new InvalidDataException("Expected Mats.");
    foreach (var mat in json.Properties())
    {
      var name = mat.Name;
      
      var diff =
        mat.Value["Diff"]
          ?.ToArray()
          .Select(it => it.Value<float>())
          .ToArray()
          .ToVec3() ?? throw new InvalidDataException("Expected Diff.");
      
      var ambi =
        mat.Value["Ambi"]
          ?.ToArray()
          .Select(it => it.Value<float>())
          .ToArray()
          .ToVec3() ?? throw new InvalidDataException("Expected Ambi.");
      
      var spec =
        mat.Value["Spec"]
          ?.ToArray()
          .Select(it => it.Value<float>())
          .ToArray()
          .ToVec3() ?? throw new InvalidDataException("Expected Spec.");

      var norm = 
        mat.Value["Norm"]
          ?.Value<string>()
          ?.Let(it =>
          {
            texes.Add(new Tex(@$"{dir}\{it}"));
            return texes.Count - 1;
          });
      
      mats.Add(
        name, 
        new Material
        {
          Diff = diff,
          Ambi = ambi,
          Spec = spec,
          Normals = norm ?? -1
        });
    }

    return (mats, texes);
  }
}

[StructLayout(LayoutKind.Sequential, Pack=4)]
public struct ObjVtx
{
  public Vec3 Pos;
  public Vec2 Uv;
  public Vec3 Norm;

  public ObjVtx(Vec3 pos, Vec2 uv, Vec3 norm)
  {
    (Pos, Uv, Norm) = (pos, uv, norm);
  }
  
  public static ObjVtx FromObj(ObjVtx vtx)
  {
    return vtx;
  }

  public static readonly Vao.Attrib[] Attribs = {
    Vao.Attrib.Float3,
    Vao.Attrib.Float2,
    Vao.Attrib.Float3
  };
}