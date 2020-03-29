// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Mapbox.Utils;
using g3;
using Project;
using System.Threading.Tasks;


public struct MoveArgs {
    public int id;
    public Vector3 pos;
    public Vector3 translate;
    public Quaternion rotate;
    public Vector3 oldPos;
    public float scale;
}

public static class PositionExtensionMethods
{
   public static Mapbox.Utils.Vector2d Vector2d(this IPosition position)
    {
        return new Mapbox.Utils.Vector2d((float)position.Latitude, (float)position.Longitude);
    }

    public static Vector2 Vector2(this IPosition position)
    {
        return new Vector2((float)position.Latitude, (float)position.Longitude);
    }

    public static Position Point(this IPosition position)
    {
        return position as Position;
    }

    public static Vector3 Vector3(this IPosition position)
    {
        return Tools.Ipos2Vect(position as Position);
    }
}

public static class LineExtensionMethods
{
    public static Position Point(this LineString line, int i)
    {
        return line.Coordinates[i] as Position;
    }

    public static Position[] Points(this LineString line)
    {
        ReadOnlyCollection<IPosition> data = line.Coordinates;
        Position[] result = new Position[data.Count];
        for (int i=0; i<data.Count; i++)
        {
            result[i] = line.Point(i);
        }
        return result;
    }
}

public class TestableObject
{
    public bool ContainsKey( string propName)
    {
        return GetType().GetMember(propName) != null;
    }
}

public static class SimpleMeshExtensions
{
    public static  Mesh ToMesh(this SimpleMesh simpleMesh )
    {
        Mesh unityMesh = new Mesh();
        Vector3[] vertices = new Vector3[simpleMesh.VertexCount];
        Color[] colors = new Color[simpleMesh.VertexCount];
        Vector2[] uvs = new Vector2[simpleMesh.VertexCount];
        Vector3[] normals = new Vector3[simpleMesh.VertexCount];
        NewVertexInfo data;
        for (int i = 0; i < simpleMesh.VertexCount; i++)
        {
            data = simpleMesh.GetVertexAll(i);
            vertices[i] = (Vector3)data.v;
            if (data.bHaveC) colors[i] = (Color)data.c;
            if (data.bHaveUV) uvs[i] = (Vector2)data.uv;
            if (data.bHaveN) normals[i] = (Vector3)data.n;
        }
        unityMesh.vertices = vertices;
        if (simpleMesh.HasVertexColors) unityMesh.colors = colors;
        if (simpleMesh.HasVertexUVs) unityMesh.uv = uvs;
        if (simpleMesh.HasVertexNormals) unityMesh.normals = normals;
        int[] triangles = new int[simpleMesh.TriangleCount * 3];
        int j = 0;
        foreach (Index3i tri in simpleMesh.TrianglesItr())
        {
            triangles[j * 3] = tri.a;
            triangles[j * 3 + 1] = tri.b;
            triangles[j * 3 + 2] = tri.c;
            j++;
        }
        unityMesh.triangles = triangles;
        return unityMesh;
    }
}

public interface ILayer
{
    RecordSet layer { get; set; }
    bool changed { get; set; }
    void Save();
}

public interface IVirgisEntity
{
    void Selected(int button);
    void UnSelected(int button);
}

public interface IVirgisComponent :IVirgisEntity
{
    void SetColor(Color color);
    //void MoveTo(Vector3 newPos);
}
