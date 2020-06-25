// copyright Runette Software Ltd, 2020. All rights reserved
using g3;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Collections.Generic;

namespace Virgis {

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

    public static class Vector3ExtebnsionMethods {
        /// <summary>
        /// Convert Vector3 World Space location to Position taking account of zoom, scale and mapscale
        /// </summary>
        /// <param name="position">Vector3 World Space coordinates</param>
        /// <returns>Position</returns>
        static public IPosition ToPosition(this Vector3 position, ICRSObject crs = null) {
            Geometry geom = position.ToGeometry();
            SpatialReference sr = new SpatialReference(null);
            if (crs == null)
                crs = new NamedCRS("EPSG:4326");
            switch (crs.Type) {
                case CRSType.Name:
                    string name = (crs as NamedCRS).Properties["name"] as string;
                    sr.SetProjCS(name);
                    break;
                case CRSType.Link:
                    string url = (crs as LinkedCRS).Properties["href"] as string;
                    sr.ImportFromUrl(url);
                    break;
                case CRSType.Unspecified:
                    sr.SetWellKnownGeogCS("EPSG:4326");
                    break;
            }
            geom.TransformTo(sr);
            double[] argout = new double[3];
            geom.GetPoint(0, argout);
            return new Position(argout[0], argout[1], argout[2]);
        }

        static public Geometry ToGeometry(this Vector3 position) {
            Geometry geom = new Geometry(wkbGeometryType.wkbPoint);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            Vector3 mapLocal = AppState.instance.map.transform.InverseTransformPoint(position);
            geom.AddPoint(mapLocal.x, mapLocal.z, mapLocal.y);
            return geom;
        }

        /// <summary>
        /// Converts Vector3 World Space Location to Point taking accoun t of zoom, scale and mapscale
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Point ToPoint(this Vector3 point) {
            return new Point(point.ToPosition());
        }
    }

    public static class PointExtensionsMethods {
        static public Vector3 ToVector3(this Point point) {
            return point.ToGeometry().Transform()[0];
        }

        static public Geometry ToGeometry(this Point point) {
            return (point.Coordinates).ToGeometry(point.CRS);
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
            return new Mapbox.Utils.Vector2d(position.Latitude, position.Longitude);
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
        /// Converts Iposition to Vector3 World Space coordinates takling account of zoom, scale and mapscale
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>Vector3</returns>
        public static Vector3 Vector3(this IPosition position, ICRSObject crs = null)
        {
            if (crs == null)
                crs = new NamedCRS("EPSG:4326");
            return position.ToGeometry(crs).Transform()[0];
        }

        public static Geometry ToGeometry(this IPosition position, ICRSObject crs) {
            Geometry geom = new Geometry(wkbGeometryType.wkbPoint);
            SpatialReference sr = new SpatialReference(null);
            if (crs == null)
                crs = new NamedCRS("EPSG:4326");
            switch (crs.Type) {
                case CRSType.Name:
                    string name = (crs as NamedCRS).Properties["name"] as string;
                    if (name.Contains("urn")) {
                        sr.ImportFromUrl(name);
                    } else if (name.Contains("EPSG")) {
                        string[] args = name.Split(':');
                        sr.ImportFromEPSG(int.Parse(args[1]));
                    } else {
                        sr.SetWellKnownGeogCS(name);
                    }
                    break;
                case CRSType.Link:
                    string url = (crs as LinkedCRS).Properties["href"] as string;
                    sr.ImportFromUrl(url);
                    break;
                case CRSType.Unspecified:
                    sr.SetWellKnownGeogCS("EPSG:4326");
                    break;
            }
            geom.AssignSpatialReference(sr);
            Nullable<double> alt = position.Altitude;
            geom.AddPoint(position.Latitude, position.Longitude, alt ?? 0.0 );
            return geom;
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
            Geometry geom = line.ToGeometry();
            return geom.Transform();
        }


        static public Geometry ToGeometry(this LineString line) {
            Geometry geom = new Geometry(wkbGeometryType.wkbLineString);
            SpatialReference sr = new SpatialReference(null);
            ICRSObject crs = line.CRS;
            if (crs == null)
                crs = new NamedCRS("EPSG:4326");
            switch (crs.Type) {
                case CRSType.Name:
                    string name = (crs as NamedCRS).Properties["name"] as string;
                    if (name.Contains("urn")) {
                        sr.ImportFromUrl(name);
                    } else if (name.Contains("EPSG")) {
                        string[] args = name.Split(':');
                        sr.ImportFromEPSG(int.Parse(args[1]));
                    } else {
                        sr.SetWellKnownGeogCS(name);
                    }
                    break;
                case CRSType.Link:
                    string url = (crs as LinkedCRS).Properties["href"] as string;
                    sr.ImportFromUrl(url);
                    break;
                case CRSType.Unspecified:
                    sr.SetWellKnownGeogCS("EPSG:4326");
                    break;
            }
            geom.AssignSpatialReference(sr);
            Position[] vertexes = line.Points();
            foreach (Position vertex in vertexes) {
                Nullable<double> alt = vertex.Altitude;
                geom.AddPoint(vertex.Latitude, vertex.Longitude, alt ?? 0.0);
            }
            return geom;
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

    /// <summary>
    ///Extension Methods to OGR Geometry
    /// </summary>
    public static class GeometryExtensions {

        /// <summary>
        /// COnvert Geometry to Vector3[] of World Space coordinates taking account of zoom
        /// </summary>
        /// <param name="geom"> Geometry</param>
        /// <returns>VEctor3[]</returns>
        public static Vector3[] Transform(this Geometry geom) {
            geom.TransformTo(AppState.instance.mapProj);
            int count = geom.GetPointCount();
            List<Vector3> ret = new List<Vector3>();
            for (int i = 0; i < count; i++) {
                double[] argout = new double[3];
                geom.GetPoint(i, argout);
                Vector3 mapLocal = new Vector3((float) argout[0], (float) argout[2], (float) argout[1]);
                ret.Add(AppState.instance.map.transform.TransformPoint(mapLocal));
            }
            return ret.ToArray();
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


