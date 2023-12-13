using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using BepuPhysics.Collidables;
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

public static class Shapes
{
  public static Memo<TypedIndex, float> Sphere =
    new(it => Penki.Simulation.Shapes.Add(new Sphere(it)));
}

public class Model : IReloadable
{
  public static readonly Lazy<Model> Sphere =
    new(() => new Model(@"Res\Models\Sphere.obj"));
  
  private static readonly Lazy<Shader> _wireframe =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Model.vsh"),
        (ShaderType.GeometryShader, @"Res\Shaders\Wireframe.gsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Lines.fsh")));
  
  private static readonly Lazy<Shader> _real =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Model.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Model.fsh")));

  private static readonly Lazy<Shader> _depth =
    new(() =>
      new Shader(
        (ShaderType.VertexShader, @"Res\Shaders\Model.vsh"),
        (ShaderType.FragmentShader, @"Res\Shaders\Depth.fsh")));
  
  public static readonly Memo<Shader, RenderSource> Shader = 
    new(source => source switch
    {
      RenderSource.Lightmap => _depth.Get,
      RenderSource.World when Penki.Wireframe => _wireframe.Get,
      RenderSource.World when !Penki.Wireframe => _real.Get,
      _ => throw new UnreachableException("uh")
    });
  
  public readonly List<ObjObj> Objs = new();
  private List<Tex> _texes;
  private readonly string dir, filename;
  
  public Model(string path)
  {
    var txt = File.ReadAllLines(path);
    var verts = new List<Vec3>();
    var uvs = new List<Vec2>();
    var norms = new List<Vec3>();
    verts.Add(Vec3.Zero);
    uvs.Add(Vec2.Zero);
    norms.Add(Vec3.Zero);
    
    var vertOff = 0;
    var uvOff = 0;
    var normOff = 0;
    var finalVerts = new List<ObjVtx>();
    var name = null as string;
    var (mats, texes) = 
      ReadMats(
        dir = path[..path.LastIndexOf('\\')], 
        filename = path[(path.LastIndexOf('\\') + 1)..path.LastIndexOf('.')]);
    
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
        vertOff += verts.Count - 1;
        uvOff += uvs.Count - 1;
        normOff += norms.Count - 1;
        finalVerts.Clear();
        verts.Clear();
        norms.Clear();
        uvs.Clear();
        verts.Add(Vec3.Zero);
        uvs.Add(Vec2.Zero);
        norms.Add(Vec3.Zero);

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
              it.Split("/").Select(idx => idx.Length == 0 ? 0 : int.Parse(idx.AsSpan())).ToArray())
            .Select(it => 
              new ObjVtx(
                verts[Math.Max(0, it[0] - vertOff)], 
                uvs[Math.Max(0, it[1] - uvOff)],
                norms[Math.Max(0, it[2] - normOff)]))
            .ToArray();
        
        finalVerts.AddRange(vertsOfFace);

        continue;
      }

      throw new 
        InvalidDataException($"If this is reached then that is bad.\n{line}");
    }
    
    NewObj();
    
    Reloader.Register(this);
    
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
  
  public void Draw(Mat4 model, RenderSource source)
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

      Shader[source].Bind()
        .Defaults(source)
        .Mat4("u_model", model)
        .Uint("u_id", obj.Rand)
        .Mat(obj.Mat);
      obj.Vao.Draw(PrimType.Triangles);
    }
  }
  
  private (Dictionary<string, Material>, List<Tex>) ReadMats(string dir, string fileName)
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

  public int ReloadId { get; set; }
  public void Reload()
  {
    _texes.ForEach(it => GL.DeleteTextures(1, ref it.Id));
    _texes.Clear();

    var (mats, texes) = ReadMats(dir, filename);
    _texes = texes;

    foreach (var obj in Objs)
    {
      obj.Mat = mats[obj.Name];
    }
  }
}

[StructLayout(LayoutKind.Sequential, Pack=4)]
public struct ObjVtx(Vec3 pos, Vec2 uv, Vec3 norm) : IVertex
{
  public Vec3 Pos = pos;
  public Vec2 Uv = uv;
  public Vec3 Norm = norm;

  public static ObjVtx FromObj(ObjVtx vtx)
  {
    return vtx;
  }

  public static readonly Vao.Attrib[] Attribs = {
    Vao.Attrib.Float3,
    Vao.Attrib.Float2,
    Vao.Attrib.Float3
  };

  public Vec3 GetPos()
  {
    return Pos;
  }
}