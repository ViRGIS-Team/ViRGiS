// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using UnityEngine;
using GeoJSON.Net.Geometry;
using g3;
using Mapbox.Unity.Utilities;


namespace Virgis
{

    /// <summary>
    /// Structure used to hold the details of a generic move request sent to a target enitity
    /// </summary>
    public struct MoveArgs
    {
        public Guid id; // id of the sending entity
        public Vector3 pos; // OPTIONAL point to move TO in world space coordinates
        public Vector3 translate; // OPTIONSAL translation in world units to be applied to target
        public Quaternion rotate; // OPTIONAL rotation to be applied to target
        public Vector3 oldPos; // OPTIONAL point to move from
        public float scale; // OPTIONAL change in scale to apply to target
    }

    /// <summary>
    /// Enum holding the types of "selection"tha the user can make
    /// </summary>
    public enum SelectionTypes
    {
        SELECT,     // Select a sing;le vertex
        SELECTALL,  // Select all verteces
        BROADCAST   // Selection event rebroadcast by parent event. DO NOT retransmit to avoid endless circles
    }

    public static class UnityLayers {
        public static LayerMask POINT { 
            get {
                return LayerMask.GetMask("Pointlike Entities");
            }
        }
        public static LayerMask LINE { 
            get {
                return LayerMask.GetMask("Linelike Entities");
            } 
        }
        public static LayerMask SHAPE {
            get {
                return LayerMask.GetMask("Shapelike Entities");
            }
        }
        public static LayerMask MESH {
            get {
                return LayerMask.GetMask("Meshlike Entities");
            }
        }
    }

    public static class Vector3ExtensionMethods {
        /// <summary>
        /// Convert Vector3 World Space location to Position taking account of zoom, scale and mapscale
        /// </summary>
        /// <param name="position">Vector3 World Space coordinates</param>
        /// <returns>Position</returns>
        static public IPosition ToPosition(this Vector3 position) {
            Vector3 mapLocal = AppState.instance.map.transform.InverseTransformPoint(position);
            Mapbox.Utils.Vector2d _latlng = VectorExtensions.GetGeoPosition(mapLocal, AppState.instance.abstractMap.CenterMercator, AppState.instance.abstractMap.WorldRelativeScale);
            return new Position(_latlng.x, _latlng.y, mapLocal.y);
        }

        /// <summary>
        /// Converts Vector3 World Space Location to Point taking accoun t of zoom, scale and mapscale
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Point ToPoint(this Vector3 position) {
            return new Point(position.ToPosition());
        }
    }

    public static class PositionExtensionMethods
    {
        /// <summary>
        /// Converts Iposition to Vector2D
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>Mapbox.Utils.Vector2d</returns>
        public static Mapbox.Utils.Vector2d Vector2d(this IPosition position)
        {
            return new Mapbox.Utils.Vector2d((float)position.Latitude, (float)position.Longitude);
        }

        /// <summary>
        /// Converts IPosition to UnityEngine.vector2
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>UnityEngine.Vector2</returns>
        public static Vector2 Vector2(this IPosition position)
        {
            return new Vector2((float)position.Latitude, (float)position.Longitude);
        }

        /// <summary>
        /// Converts IPositon to Position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Position Point(this IPosition position)
        {
            return position as Position;
        }

        /// <summary>
        /// Converts Iposition to Vector3 World Space coordinates takling account of zoom, scale and mapscale
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>Vector3</returns>
        public static Vector3 Vector3(this IPosition position)
        {
            float Alt;
            if (position.Altitude == null) {
                Alt = 0.0f;
            } else {
                Alt = (float) position.Altitude;
            };
            Vector3 mapLocal = Conversions.GeoToWorldPosition(position.Latitude, position.Longitude, AppState.instance.abstractMap.CenterMercator, AppState.instance.abstractMap.WorldRelativeScale).ToVector3xz();
            mapLocal.y = Alt;
            return AppState.instance.map.transform.TransformPoint(mapLocal);
        }
    }

    public static class LineExtensionMethods
    {
        /// <summary>
        /// Converts LineString Vertex i to a Position
        /// </summary>
        /// <param name="line">LineString</param>
        /// <param name="i">vertex index</param>
        /// <returns>Position</returns>
        public static Position Point(this LineString line, int i)
        {
            return line.Coordinates[i] as Position;
        }

        /// <summary>
        /// Converts LineString to Position[]
        /// </summary>
        /// <param name="line">LineString</param>
        /// <returns>Position[]</returns>
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

        /// <summary>
        /// Converts LineString to Vector3[] in world space taking account of zoom, scale and map scale
        /// </summary>
        /// <param name="line">LineString</param>
        /// <returns>Vector3[] World Space Locations</returns>
        static public Vector3[] Vector3(this LineString line) {
            Vector3[] result = new Vector3[line.Coordinates.Count];
            for (int i = 0; i < line.Coordinates.Count; i++) {
                result[i] = line.Point(i).Vector3();
            }
            return result;
        }
    }

    public static class DcurveExtensions
    {
        /// <summary>
        /// Creates g3.DCurve from Vector3[]
        /// </summary>
        /// <param name="curve">DCurve</param>
        /// <param name="verteces">Vextor3[]</param>
        /// <param name="bClosed">whether the line is closed</param>
        public static void Vector3(this g3.DCurve3 curve, Vector3[] verteces, bool bClosed)
        {
            curve.ClearVertices();
            curve.Closed = bClosed;
            foreach (Vector3 vertex in verteces)
            {
                curve.AppendVertex(vertex);
            }
        }

        /// <summary>
        /// Estimates the 3D centroid of a DCurve 
        /// </summary>
        /// <param name="curve">DCurve</param>
        /// <returns>Vector3[]</returns>
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

        /// <summary>
        /// Estimates the nearest point on a DCurve to the centroid of that DCurve
        /// </summary>
        /// <param name="curve">g3.DCurve</param>
        /// <returns>g3.Vector3d Centroid</returns>
        public static Vector3d CenterMark(this DCurve3 curve)
        {
            Vector3d center = curve.Center();
            return curve.GetSegment(curve.NearestSegment(center)).NearestPoint(center);
        }

        /// <summary>
        /// Finds the Segment from the DCurve3 closes to the position
        /// </summary>
        /// <param name="curve">DCurve3</param>
        /// <param name="position">Vector3d</param>
        /// <returns>Integer Sgement index</returns>
        public static int NearestSegment (this DCurve3 curve, Vector3d position) {
            _ = curve.DistanceSquared(position, out int iSeg, out double tangent);
            return iSeg;
        }
    }


    public static class PolygonExtensions {

        public static GeneralPolygon2d ToPolygon(this List<Dataline> list, ref Frame3f frame) {
            List<VertexLookup> VertexTable = list[0].VertexTable;
            Vector3d[] vertices = new Vector3d[VertexTable.Count];
            for (int j = 0; j < VertexTable.Count; j++) {
                vertices[j] = VertexTable.Find(item => item.Vertex == j).Com.transform.position;
            }
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(vertices);
            frame = new Frame3f(orth.Origin, orth.Normal);
            GeneralPolygon2d poly = new GeneralPolygon2d(new Polygon2d());
            for (int i = 0; i<list.Count; i++) {
                VertexTable = list[i].VertexTable;
                vertices = new Vector3d[VertexTable.Count];
                for (int j = 0; j < VertexTable.Count; j++) {
                    vertices[j] = VertexTable.Find(item => item.Vertex == j).Com.transform.position;
                }
                List<Vector2d> vertices2d = new List<Vector2d>();
                foreach (Vector3d v in vertices) {
                    Vector2f vertex = frame.ToPlaneUV((Vector3f) v, 3);
                    if (i != 0 && !poly.Outer.Contains(vertex))
                        break;
                    vertices2d.Add(vertex);
                }
                Polygon2d p2d = new Polygon2d(vertices2d);
                if (i == 0) {
                    p2d = new Polygon2d(vertices2d);
                    p2d.Reverse();
                    poly.Outer = p2d;
                } else {
                    try {
                        poly.AddHole(p2d, true, true);
                    } catch {
                        p2d.Reverse();
                        poly.AddHole(p2d, true, true);
                    }
                   
                }
            }
            return poly;
        }
    }


    public static class SimpleMeshExtensions
    {
        /// <summary>
        /// Converts g3.SimpleMesh to UnityEngine.Mesh
        /// </summary>
        /// <param name="simpleMesh">SimpleMesh</param>
        /// <returns>UnityEngine.Mesh</returns>
        public static Mesh ToMesh(this SimpleMesh simpleMesh)
        {
            Mesh unityMesh = new Mesh();
            MeshTransforms.ConvertZUpToYUp(simpleMesh);
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
        /// <summary>
        /// Rounds a Vector3 in 3d to the nearest value divisible by roundTo
        /// </summary>
        /// <param name="vector3">Vector 3 value</param>
        /// <param name="roundTo"> rounding size</param>
        /// <returns>Vector3 rounded value</returns>
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
    /// Structure used to hold avertex for an arbitrary shape and to calculate equality
    /// </summary>
    public class VertexLookup
    {
        public Guid Id;
        public int Vertex;
        public bool isVertex;
        public VirgisFeature Com;
        public LineSegment Line;
        public int pVertex;
        
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            VertexLookup com = obj as VertexLookup;
            if (com == null) return false;
            else return Equals(com);
        }
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public bool Equals(VertexLookup other)
        {
            if (other == null) return false;
            return (this.Id.Equals(other.Id));
        }

        public int CompareTo(VertexLookup other)
        {
            if (other == null)
                return 1;

            else
                return Vertex.CompareTo(other.Vertex);
        }
    }
}


