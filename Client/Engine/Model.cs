using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.OpenGL4;
using Penki.Client.Game;
using Penki.Client.GLU;

namespace Penki.Client.Engine;

public class Material
{
  public required Vec3 Dark;
  public required Vec3 Light;
  public required Vec3 LightModel;
  public required int Normals;
  public required int Alpha;
  public float Shine = 32;
}

public class ObjObj
{
  public required List<ObjVtx> Mesh;
  public required Material Mat;
  public required string Name;
  public required Vao Vao;
  public required Buf Vbo;
  public required uint Rand;
}

public class Model
{
  public readonly List<ObjObj> Objs = new();
  private readonly List<Tex> _texes;

  public static readonly Lazy<Shader> Shader =
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
        finalVerts.Clear();
        verts.Clear();
        norms.Clear();
        uvs.Clear();

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

      Objs.Add(new ObjObj
      {
        Name = name,
        Mat = mats[name],
        Mesh = finalVerts,
        Vbo = vbo,
        Vao = vao,
        Rand = (uint)Random.Shared.Next()
      });
    }
  }
  
  public void Draw()
  {
    Draw(Mat4.Identity);
  }

  public void Draw(Mat4 model)
  {
    foreach (var obj in Objs)
    {
      if (obj.Mat.Normals != -1)
      {
        _texes[obj.Mat.Normals].Bind(TexUnit.Texture0);
      }
      
      if (obj.Mat.Alpha != -1)
      {
        _texes[obj.Mat.Alpha].Bind(TexUnit.Texture1);
      }
      
      var sh = (Shader)Shader;
      sh.Bind()
        .Defaults()
        .Mat4("u_model", model)
        .Uint("u_id", obj.Rand)
        .Mat(obj.Mat);
      obj.Vao.Draw(PrimType.Triangles);
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

      var dark = mat.Value["Dark"]?.Value<int>() ??
                 throw new Exception("Expected Dark.");
      
      var light = mat.Value["Light"]?.Value<int>() ??
                  throw new Exception("Expected Light.");
      
      var lightModel =
        mat.Value["LightModel"]
          ?.ToArray()
          .Select(it => it.Value<float>())
          .ToArray()
          .ToVec3() ?? throw new InvalidDataException("Expected LightModel.");

      var norm = 
        mat.Value["Norm"]
          ?.Value<string>()
          ?.Let(it =>
          {
            texes.Add(new Tex(@$"{dir}\{it}"));
            return texes.Count - 1;
          });
      
      var alpha = 
        mat.Value["Alpha"]
          ?.Value<string>()
          ?.Let(it =>
          {
            texes.Add(new Tex(@$"{dir}\{it}"));
            return texes.Count - 1;
          });

      var shine =
        mat.Value["Shine"]
          ?.Value<float>()
        ?? 32.0f;
      
      mats.Add(
        name, 
        new Material
        {
          Dark = DreamyHaze.Colors[dark],
          Light = DreamyHaze.Colors[light],
          LightModel = lightModel,
          Normals = norm ?? -1,
          Shine = shine,
          Alpha = alpha ?? -1
        });
    }

    return (mats, texes);
  }
}

// public class InstancedModel<T> : Model
//   where T : struct, IVertex
// {
//   private readonly List<T> _instanceData;
//   
//   public InstancedModel(string path) : base(path)
//   {
//     
//   }
// }

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