// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.ObjectModel;
using UnityEngine;
using GeoJSON.Net.Geometry;
using g3;

namespace Virgis
{

    /// <summary>
    /// Structure used to hold the details of a generic move request sent to a target enitity
    /// </summary>
    public struct MoveArgs
    {
        public int id; // id of the sending entity
        public Vector3 pos; // OPTIONAL point to move TO in world space coordinates
        public Vector3 translate; // OPTIONSAL translation in world units to be applied to target
        public Quaternion rotate; // OPTIONAL rotation to be applied to target
        public Vector3 oldPos; // OPTIONAL point to move from
        public float scale; // OPTIONAL change in scale to apply to target
    }

    public enum SelectionTypes
    {
        SELECT,     // Select a sing;le vertex
        SELECTALL,  // Select all verteces
        BROADCAST   // Selection event rebroadcast by parent event. DO NOT retransmit to avoid endless circles
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
            for (int i = 0; i < data.Count; i++)
            {
                result[i] = line.Point(i);
            }
            return result;
        }
    }

    public static class DcurveExtensions
    {
        public static void Vector3(this DCurve3 curve, Vector3[] verteces, bool bClosed)
        {
            curve.Closed = bClosed;
            foreach (Vector3 vertex in verteces)
            {
                curve.AppendVertex(vertex);
            }
        }

        public static Vector3d Center(this DCurve3 curve)
        {
            Vector3d center = Vector3d.Zero;
            int len = curve.SegmentCount;
            if (!curve.Closed) len++;
            for (int i = 0; i < len; i++)
            {
                center += curve.GetVertex(i);
            }
            center /= len;
            return center;
        }

        public static Vector3d CenterMark(this DCurve3 curve)
        {
            _ = curve.DistanceSquared(curve.Center(), out int iSeg, out double tangent);
            Segment3d seg = curve.GetSegment(iSeg);
            return seg.Center;
        }
    }



    public static class SimpleMeshExtensions
    {
        public static Mesh ToMesh(this SimpleMesh simpleMesh)
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

    public static class VirgisVectorExtensions
    {
        public static Vector3 Round(this Vector3 vector3, float roundTo = 0.1f)
        {
            return new Vector3(
                Mathf.Round(vector3.x / roundTo) * roundTo,
                Mathf.Round(vector3.y / roundTo) * roundTo,
                Mathf.Round(vector3.z / roundTo) * roundTo
                );
        }
    }


    /// <summary>
    /// Abstract parent for all in game entities
    /// </summary>
    public interface IVirgisEntity
    {
        void Selected(SelectionTypes button);
        void UnSelected(SelectionTypes button);
        void EditEnd();
    }

    /// <summary>
    /// Abstract Parent for all symbology relevant in game entities
    /// </summary>
    public interface IVirgisComponent : IVirgisEntity
    {
        void SetColor(Color color);
        //void MoveTo(Vector3 newPos);
    }

    /// <summary>
    /// abstract parent for generic datasets
    /// </summary>
    public abstract class DataObject { }
}


