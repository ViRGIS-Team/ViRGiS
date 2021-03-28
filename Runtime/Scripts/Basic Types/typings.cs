// copyright Runette Software Ltd, 2020. All rights reserved
using g3;
using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using CoordinateTransformation = OSGeo.OSR.CoordinateTransformation;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using DelaunatorSharp;
using System.Linq;
using DXF = netDxf;
using Mdal;

namespace Virgis {

    /// <summary>
    /// Structure used to hold the details of a generic move request sent to a target enitity
    /// </summary>
    public struct MoveArgs {
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
    public enum SelectionType {
        SELECT,     // Select a sing;le vertex
        SELECTALL,  // Select all verteces
        INFO,       // Slection Actin related to the Info screen
        BROADCAST   // Selection event rebroadcast by parent event. DO NOT retransmit to avoid endless circles
    }

    public enum FeatureType {
        POINT,
        LINE,
        POLYGON,
        MESH,
        POINTCLOUD,
        RASTER,
        NONE

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



    public static class DcurveExtensions {
        /// <summary>
        /// Creates g3.DCurve from Vector3[]
        /// </summary>
        /// <param name="curve">DCurve</param>
        /// <param name="verteces">Vextor3[]</param>
        /// <param name="bClosed">whether the line is closed</param>
        public static DCurve3 Vector3(this DCurve3 curve, Vector3[] verteces, bool bClosed) {
            curve.ClearVertices();
            curve.Closed = bClosed;
            foreach (Vector3 vertex in verteces) {
                curve.AppendVertex(vertex);
            }
            return curve;
        }

        /// <summary>
        /// Creates ag3.DCurve from a geometry
        /// </summary>
        /// <param name="curve"> this curve</param>
        /// <param name="geom"> the OGR geometry to ue as ths source</param>
        /// <param name="crs"> the crs to u for the DCurve3 DEFAULT map default projections or EPG:4326 if none</param>
        /// <returns></returns>
        public static DCurve3 FromGeometry(this DCurve3 curve, Geometry geom, SpatialReference crs = null) {
            if (geom.GetSpatialReference() == null) {
                if (crs != null) {
                    geom.AssignSpatialReference(crs);
                } else {
                    geom.AssignSpatialReference(AppState.instance.projectCrs);
                }

            }
            if (geom.TransformTo(AppState.instance.mapProj) != 0)
                throw new NotSupportedException("projection failed");
            if (geom.Transform(AppState.instance.mapTrans) != 0)
                throw new NotSupportedException("axis change failed");
            int n = geom.GetPointCount();
            Vector3d[] ls = new Vector3d[n];
            for (int i = 0; i < n; i++) {
                double[] argout = new double[3];
                geom.GetPoint(i, argout);
                ls[i] = new Vector3d(argout);
            }
            curve.ClearVertices();
            curve.SetVertices(ls);
            curve.Closed = geom.IsRing();
            return curve;
        }

        /// <summary>
        /// Converts DCurve3 whihc is in Local Vector3d coordinates to Vector3[] World coordinates 
        /// </summary>
        /// <param name="curve">input curve</param>
        /// <returns>Vector3[] in world coordinates</returns>
        public static Vector3[] ToWorld(this DCurve3 curve) {
            List<Vector3> ret = new List<Vector3>();
            List<Vector3d> vertexes = curve.Vertices as List<Vector3d>;
            for (int i = 0; i < curve.VertexCount; i++) {
                Vector3 local = (Vector3) vertexes[i];
                ret.Add(AppState.instance.map.transform.TransformVector(local));
            }
            return ret.ToArray();
        }

        /// <summary>
        /// Calculates the 3D Centroid as a World space Vector3 of the DCurve3 that is in local map space.
        /// </summary>
        /// <param name="curve">DCurve3 in local map space coordinates</param>
        /// <returns>Vcetor3 in world space coordinates</returns>
        public static Vector3 WorldCenter(this DCurve3 curve) {
            return AppState.instance.map.transform.TransformVector((Vector3) curve.Center());
        }
        /// <summary>
        /// Estimates the 3D centroid of a DCurve 
        /// </summary>
        /// <param name="curve">DCurve</param>
        /// <returns>Vector3[]</returns>
        public static Vector3d Center(this DCurve3 curve) {
            Vector3d center = Vector3d.Zero;
            int len = curve.SegmentCount;
            if (!curve.Closed) len++;
            for (int i = 0; i < len; i++) {
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
        public static Vector3d CenterMark(this DCurve3 curve) {
            Vector3d center = curve.Center();
            return curve.GetSegment(curve.NearestSegment(center)).NearestPoint(center);
        }

        /// <summary>
        /// Finds the Segment from the DCurve3 closes to the position
        /// </summary>
        /// <param name="curve">DCurve3</param>
        /// <param name="position">Vector3d</param>
        /// <returns>Integer Sgement index</returns>
        public static int NearestSegment(this DCurve3 curve, Vector3d position) {
            _ = curve.DistanceSquared(position, out int iSeg, out double tangent);
            return iSeg;
        }

        public static List<Vector3d> AllVertexItr(this List<DCurve3> poly) {
            List<Vector3d> ret = new List<Vector3d>();
            foreach (DCurve3 curve in poly) {
                ret.AddRange(curve.Vertices);
            }
            return ret;
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
        public static Vector3[] TransformWorld(this Geometry geom, SpatialReference crs = null) {
            if (geom.GetCoordinateDimension() == 2) {
                geom.Set3D(1);
            };
            if (geom.GetSpatialReference() == null) {
                if (crs != null) {
                    geom.AssignSpatialReference(crs);
                } else {
                    geom.AssignSpatialReference(AppState.instance.projectCrs);
                }
            }
            if (geom.TransformTo(AppState.instance.mapProj) != 0)
                throw new NotSupportedException("projection failed");
            if (geom.Transform(AppState.instance.mapTrans) != 0)
                throw new NotSupportedException("axis change failed");
            int count = geom.GetPointCount();
            List<Vector3> ret = new List<Vector3>();
            if (count > 0)
                for (int i = 0; i < count; i++) {
                    double[] argout = new double[3];
                    geom.GetPoint(i, argout);
                    Vector3 mapLocal = (Vector3) new Vector3d(argout);
                    ret.Add(AppState.instance.map.transform.TransformPoint(mapLocal));
                }
            else {
                throw new NotSupportedException("no Points in geometry");
            }
            return ret.ToArray();
        }

        public static Geometry Vector3(this Geometry geom, Vector3[] points) {
            foreach (Vector3 point in points) {
                Vector3 mapLocal = AppState.instance.map.transform.InverseTransformPoint(point);
                geom.AddPoint(mapLocal.x, mapLocal.z, mapLocal.y);
            }
            return geom;
        }
    }
    public static class PolygonExtensions {

        public static GeneralPolygon2d ToPolygon(this List<DCurve3> list, ref Frame3f frame) {
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(list[0].Vertices);
            frame = new Frame3f(orth.Origin, orth.Normal);
            GeneralPolygon2d poly = new GeneralPolygon2d(new Polygon2d());
            for (int i = 0; i < list.Count; i++) {
                List<Vector3d> vertices = list[i].Vertices.ToList();
                List<Vector2d> vertices2d = new List<Vector2d>();
                foreach (Vector3d v in vertices) {
                    Vector2f vertex = frame.ToPlaneUV((Vector3f) v, 3);
                    if (i != 0 && !poly.Outer.Contains(vertex)) break;
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

        public static bool IsOutside(this GeneralPolygon2d poly, Segment2d seg) {
            bool isOutside = true;
            if (poly.Outer.IsMember(seg, out isOutside)) {
                if (isOutside)
                    return true;
                else
                    return false;
            }
            foreach (Polygon2d hole in poly.Holes) {
                if (hole.IsMember(seg, out isOutside)) {
                    if (isOutside)
                        return true;
                    else
                        return false;
                }
            }
            return false;
        }

        public static bool BiContains(this Polygon2d poly, Segment2d seg) {
            foreach (Segment2d thisSeg in poly.SegmentItr()) {
                if (thisSeg.BiEquals(seg))
                    return true;
            }
            return false;
        }

        public static bool IsMember(this Polygon2d poly, Segment2d seg, out bool IsOutside) {
            IsOutside = true;
            if (poly.Vertices.Contains(seg.P0) && poly.Vertices.Contains(seg.P1)) {
                if (poly.BiContains(seg))
                    IsOutside = false;
                return true;
            }
            return false;
        }
    }

    public static class DelaunatorExtensions {
        public static IPoint[] ToPoints(this IEnumerable<Vector2d> vertices) => vertices.Select(vertex => new Point(vertex.x, vertex.y)).OfType<IPoint>().ToArray();

        public static Vector2d[] ToVectors2d(this IEnumerable<IPoint> points) => points.Select(point => point.ToVector2d()).ToArray();

        public static Vector2d ToVector2d(this IPoint point) => new Vector2d((float) point.X, (float) point.Y);

        public static Vector2d CetIncenter(this ITriangle tri) {
            Vector2d A = tri.Points.ElementAt<IPoint>(0).ToVector2d();
            Vector2d B = tri.Points.ElementAt<IPoint>(1).ToVector2d();
            Vector2d C = tri.Points.ElementAt<IPoint>(2).ToVector2d();
            double a = (B - A).Length;
            double b = (C - B).Length;
            double c = (A - C).Length;
            double x = (a * A.x + b * B.x + c * C.x) / (a + b + c);
            double y = (a * A.y + b * B.y + c * C.y) / (a + b + c);
            return new Vector2d(x, y);
        }

    }

    public static class Segment2dExtensions {
        public static bool BiEquals(this Segment2d self, Segment2d seg) {
            return seg.Center == self.Center && seg.Extent == self.Extent;
        }
    }

    public static class MeshExtensions {

        /// <summary>
        /// Craate a new compact DMesh3 with all of the ViRGiS metadata copied across
        /// </summary>
        /// <param name="dMesh">Source DMesh3</param>
        /// <returns>DMesh3</returns>
        public static DMesh3 Compactify(this DMesh3 dMesh) {
            DMesh3 mesh = new DMesh3(dMesh);
            //mesh.CompactCopy(dMesh);

            if (dMesh.HasMetadata) {
                string crs = dMesh.FindMetadata("CRS") as string;
                if (crs != null)
                    mesh.AttachMetadata("CRS", crs);
            }
            return mesh;
        }

        /// <summary>
        /// Converts g3.DMesh3 to UnityEngine.Mesh.
        /// The DMesh3 must be compact. If neccesary - run Compactify first.
        /// </summary>
        /// <param name="mesh">Dmesh3</param>
        /// <param name="project"> Should the mesh be projected into virgi projection DEFAULT true</param>
        /// <returns>UnityEngine.Mesh</returns>
        public static Mesh ToMesh(this DMesh3 mesh, Boolean project = true) {
            Mesh unityMesh = new Mesh();
            unityMesh.MarkDynamic();
            unityMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            if (project && !mesh.Transform(AppState.instance.mapProj)) throw new Exception("Mesh Projection Failed");
            Vector3[] vertices = new Vector3[mesh.VertexCount];
            Color[] colors = new Color[mesh.VertexCount];
            Vector2[] uvs = new Vector2[mesh.VertexCount];
            Vector3[] normals = new Vector3[mesh.VertexCount];
            NewVertexInfo data;
            for (int i = 0; i < mesh.VertexCount; i++) {
                if (mesh.IsVertex(i)) {
                    data = mesh.GetVertexAll(i);
                    vertices[i] = (Vector3) data.v;
                    if (data.bHaveC)
                        colors[i] = (Color) data.c;
                    if (data.bHaveUV)
                        uvs[i] = (Vector2) data.uv;
                    if (data.bHaveN)
                        normals[i] = (Vector3) data.n;
                }
            }
            unityMesh.vertices = vertices;
            if (mesh.HasVertexColors) unityMesh.SetColors(colors);
            if (mesh.HasVertexUVs) unityMesh.SetUVs(0, uvs);
            if (mesh.HasVertexNormals) unityMesh.SetNormals(normals);
            int[] triangles = new int[mesh.TriangleCount * 3];
            int j = 0;
            foreach (Index3i tri in mesh.Triangles()) {
                triangles[j * 3] = tri.a;
                triangles[j * 3 + 1] = tri.b;
                triangles[j * 3 + 2] = tri.c;
                j++;
            }
            unityMesh.triangles = triangles;
            return unityMesh;
        }

        public static bool Transform(this DMesh3 dMesh, SpatialReference to) {
            string crs = dMesh.FindMetadata("CRS") as string;
            if (crs != null && crs != "") {
                SpatialReference from = new SpatialReference(null);
                if (crs.Contains("+proj")) {
                    from.ImportFromProj4(crs);
                } else if (crs.Contains("epsg") || crs.Contains("EPSG")) {
                    int epsg = int.Parse(crs.Split(':')[1]);
                    from.ImportFromEPSG(epsg);
                } else {
                    from.ImportFromWkt(ref crs);
                };
                try {
                    CoordinateTransformation trans = new CoordinateTransformation(from, to);
                    for (int i = 0; i < dMesh.VertexCount; i++) {
                        if (dMesh.IsVertex(i)) {
                            Vector3d vertex = dMesh.GetVertex(i);
                            double[] dV = new double[3] { vertex.x, vertex.y, vertex.z };
                            trans.TransformPoint(dV);
                            AppState.instance.mapTrans.TransformPoint(dV);
                            dMesh.SetVertex(i, new Vector3d(dV));
                        }
                    };
                    return true;
                } catch {
                    return false;
                }
            }
            try {
                for (int i = 0; i < dMesh.VertexCount; i++) {
                    if (dMesh.IsVertex(i)) {
                        Vector3d vertex = dMesh.GetVertex(i);
                        double[] dV = new double[3] { vertex.x, vertex.y, vertex.z };
                        AppState.instance.mapTrans.TransformPoint(dV);
                        dMesh.SetVertex(i, new Vector3d(dV));
                    }
                };
                return true;
            } catch {
                return false;
            }
        }

        public static void CalculateUVs(this DMesh3 dMesh) {
            dMesh.EnableVertexUVs(Vector2f.Zero);
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(dMesh.Vertices());
            Frame3f frame = new Frame3f(orth.Origin, orth.Normal);
            AxisAlignedBox3d bounds = dMesh.CachedBounds;
            AxisAlignedBox2d boundsInFrame = new AxisAlignedBox2d();
            for (int i = 0; i < 8; i++) {
                boundsInFrame.Contain(frame.ToPlaneUV((Vector3f) bounds.Corner(i), 3));
            }
            Vector2f min = (Vector2f) boundsInFrame.Min;
            float width = (float) boundsInFrame.Width;
            float height = (float) boundsInFrame.Height;

            for (int i = 0; i < dMesh.VertexCount; i++) {
                Vector2f UV = frame.ToPlaneUV((Vector3f) dMesh.GetVertex(i), 3);
                UV.x = (UV.x - min.x) / width;
                UV.y = (UV.y - min.y) / height;
                dMesh.SetVertexUV(i, UV);
            }
        }

        public static Task<int> CalculateUVsAsync(this DMesh3 dMesh) {

            TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
            Task<int> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {
                dMesh.CalculateUVs();
                tcs1.SetResult(1);
            });
            return t1;
        }
    }

    public static class VirgisVectorExtensions {
        /// <summary>
        /// Rounds a Vector3 in 3d to the nearest value divisible by roundTo
        /// </summary>
        /// <param name="vector3">Vector 3 value</param>
        /// <param name="roundTo"> rounding size</param>
        /// <returns>Vector3 rounded value</returns>
        public static Vector3 Round(this Vector3 vector3, float roundTo = 0.1f) {
            return new Vector3(
                Mathf.Round(vector3.x / roundTo) * roundTo,
                Mathf.Round(vector3.y / roundTo) * roundTo,
                Mathf.Round(vector3.z / roundTo) * roundTo
                );
        }

        /// <summary>
        /// Cibvert vector3D in Y-up coordinate frame to a netDXF Vector3 in z-up coordinate frame
        /// using the optional CoordinateTranform to reproject the dpoint if present
        /// </summary>
        /// <param name="v"> Vector3d</param>
        /// <param name="transform">Coordinate Transform</param>
        /// <returns>DXF.Vector3</returns>
        public static DXF.Vector3 ToDxfVector3(this Vector3d v, CoordinateTransformation transform = null) {
            double[] vlocal = new double[3] { v.x, v.z, v.y };
            if (transform != null) {
                transform.TransformPoint(vlocal);
            }
            return new DXF.Vector3(vlocal[0], vlocal[1], vlocal[2]);
        }
    }
    public static class FeatureExtensions {

        public static bool ContainsKey(this Feature feature, string name) {
            int fieldCount = feature.GetFieldCount();
            bool flag = false;
            for (int i = 0; i < fieldCount; i++) {
                FieldDefn fd = feature.GetFieldDefnRef(i);
                if (fd.GetName() == name) {
                    flag = true;
                    break;
                }
            }
            return flag;
        }

        public static object Get(this Feature feature, string name) {
            int fieldCount = feature.GetFieldCount();
            object ret = null;
            for (int i = 0; i < fieldCount; i++) {
                FieldDefn fd = feature.GetFieldDefnRef(i);
                if (fd.GetName() == name) {
                    FieldType ft = fd.GetFieldType();
                    switch (ft) {
                        case FieldType.OFTString:
                            ret = feature.GetFieldAsString(i);
                            break;
                        case FieldType.OFTReal:
                            ret = feature.GetFieldAsDouble(i);
                            break;
                        case FieldType.OFTInteger:
                            ret = feature.GetFieldAsInteger(i);
                            break;
                    }
                }
            }
            return ret;
        }

        public static Dictionary<string, object> GetAll(this Feature feature) {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (feature != null) {
                int fieldCount = feature.GetFieldCount();
                for (int i = 0; i < fieldCount; i++) {
                    FieldDefn fd = feature.GetFieldDefnRef(i);
                    string key = fd.GetName();
                    object value = null;
                    FieldType ft = fd.GetFieldType();
                    switch (ft) {
                        case FieldType.OFTString:
                            value = feature.GetFieldAsString(i);
                            break;
                        case FieldType.OFTReal:
                            value = feature.GetFieldAsDouble(i);
                            break;
                        case FieldType.OFTInteger:
                            value = feature.GetFieldAsInteger(i);
                            break;
                    }
                    ret.Add(key, value);
                }
            }
            return ret;
        }

        public static void Set(this Feature feature, string name, double value) {
            int i = feature.GetFieldIndex(name);
            if (i > -1) {
                feature.SetField(name, value);
            }
        }

        public static void Set(this Feature feature, string name, string value) {
            int i = feature.GetFieldIndex(name);
            if (i > -1) {
                feature.SetField(name, value);
            }
        }

        public static void Set(this Feature feature, string name, int value) {
            int i = feature.GetFieldIndex(name);
            if (i > -1) {
                feature.SetField(name, value);
            }
        }
    }

    /// <summary>
    /// Structure used to hold avertex for an arbitrary shape and to calculate equality
    /// </summary>
    public class VertexLookup {
        public Guid Id;
        public int Vertex;
        public bool isVertex;
        public VirgisFeature Com;
        public LineSegment Line;
        public int pVertex;

        public override bool Equals(object obj) {
            if (obj == null) return false;
            VertexLookup com = obj as VertexLookup;
            if (com == null) return false;
            else return Equals(com);
        }
        public override int GetHashCode() {
            return Id.GetHashCode();
        }
        public bool Equals(VertexLookup other) {
            if (other == null) return false;
            return (this.Id.Equals(other.Id));
        }

        public int CompareTo(VertexLookup other) {
            if (other == null)
                return 1;

            else
                return Vertex.CompareTo(other.Vertex);
        }
    }


    public static class DXFExtensions {

        /// <summary>
        /// Convert a netDXF Vector3 in z-up coordinate frame to a Vectir3D in Y-up coordinate frame
        /// using the optional Coordinate Transform to reproject the point if present
        /// </summary>
        /// <param name="v">DXF.Vector3</param>
        /// <param name="ct">Coordinate transform</param>
        /// <returns>Vector3d</returns>
        public static Vector3d ToVector3d(this DXF.Vector3 v, CoordinateTransformation ct = null) {
            double[] vlocal = new double[3] { v.X, v.Y, v.Z };
            if (ct != null) {
                ct.TransformPoint(vlocal);
            }
            return new Vector3d((float) vlocal[0], (float) vlocal[2], (float) vlocal[1]);
        }
    }

    public class Convert {

        public static SpatialReference TextToSR(string str) {
            if (str.Contains("epsg:") || str.Contains("EPSG:")) {
                SpatialReference crs = new SpatialReference(null);
                string[] parts = str.Split(':');
                crs.ImportFromEPSG(int.Parse(parts[1]));
                return crs;
            }
            if (str.Contains("proj")) {
                SpatialReference crs = new SpatialReference(null);
                crs.ImportFromProj4(str);
                return crs;
            }
            return new SpatialReference(str);
        }
    }

    /// <summary>
    /// from http://www.stevevermeulen.com/index.php/2017/09/using-async-await-in-unity3d-2017/
    /// </summary>
    public static class TaskExtensions {
        public static IEnumerator AsIEnumerator(this Task task) {
            while (!task.IsCompleted) {
                yield return null;
            }

            if (task.IsFaulted) {
                throw task.Exception;
            }
        }
    }
}