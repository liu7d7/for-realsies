﻿using System.Buffers;
using System.Diagnostics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace Penki.Client.Engine;

public static class Utils
{
  public static (int[], int) QuadIndices(int width, int height)
  {
    var indices = ArrayPool<int>.Shared.Rent(width * height * 6);
    
    for (int i = 0; i < height; i++)
    for (int j = 0; j < width; j++)
    {
      var baseIdx = (i * width + j) * 6;
      indices[baseIdx + 0] = (i + 1) * (width + 1) + j + 1;
      indices[baseIdx + 1] = (i + 1) * (width + 1) + j;
      indices[baseIdx + 2] = i * (width + 1) + j;
      indices[baseIdx + 3] = i * (width + 1) + j;
      indices[baseIdx + 4] = i * (width + 1) + j + 1;
      indices[baseIdx + 5] = (i + 1) * (width + 1) + j + 1;
    }

    return (indices, width * height * 6);
  }
  
  public static Span<int> QuadIndicesAdj(int width, int height)
  {
    var indices = new List<int>();
    var halfEdges = new Dictionary<(int, int), int>();
    
    for (int i = 0; i < height; i++)
    for (int j = 0; j < width; j++)
    {
      var a1 = i * (width + 1) + j;
      var a2 = (i + 1) * (width + 1) + j;
      var a3 = (i + 1) * (width + 1) + j + 1;
      indices.Add(a1);
      indices.Add(a2);
      indices.Add(a3);
      halfEdges[(a1, a2)] = a3;
      halfEdges[(a2, a3)] = a1;
      halfEdges[(a3, a1)] = a2;
      
      a1 = (i + 1) * (width + 1) + j + 1;
      a2 = i * (width + 1) + j + 1;
      a3 = i * (width + 1) + j + 1;
      indices.Add(a1);
      indices.Add(a2);
      indices.Add(a3);
      halfEdges[(a1, a2)] = a3;
      halfEdges[(a2, a3)] = a1;
      halfEdges[(a3, a1)] = a2;
    }

    for (int i = 0; i < indices.Count; i += 3)
    {
      indices[i] = halfEdges.GetValueOrDefault((i + 1, i));
      indices[i + 1] = halfEdges.GetValueOrDefault((i + 2, i + 1));
      indices[i + 2] = halfEdges.GetValueOrDefault((i, i + 2));
    }

    return indices.AsSpan();
  }

  public static Buffer<Triangle> Tris<T>(Span<T> verts, int length, BufferPool pool)
    where T : struct, IVertex
  {
    Debug.Assert(length % 3 == 0);

    pool.Take<Triangle>(length / 3, out var tris);
    for (int i = 0; i < length; i += 3)
    {
      tris[i / 3] = new Triangle(
        verts[i + 2].GetPos().ToNumerics(),
        verts[i + 1].GetPos().ToNumerics(),
        verts[i + 0].GetPos().ToNumerics());
    }

    return tris;
  } 
  
  public static Buffer<Triangle> Tris<T>(Span<T> verts, Span<int> indices, int length, BufferPool pool)
    where T : struct, IVertex
  {
    Debug.Assert(length % 3 == 0);

    pool.Take<Triangle>(length / 3, out var tris);
    for (int i = 0; i < length; i += 3)
    {
      tris[i / 3] = new Triangle(
        verts[indices[i + 2]].GetPos().ToNumerics(),
        verts[indices[i + 1]].GetPos().ToNumerics(),
        verts[indices[i + 0]].GetPos().ToNumerics());
    }

    return tris;
  } 
}