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
        BROADCAST   // Selection event rebroadcast by parent event. DO NOT retransmit to avoid endless circles
    }

    public enum FeatureType {
        POINT,
        LINE,
        POLYGON,
        MESH,
        POINTCLOUD,
        RASTER
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
            if (crs == null) {
                sr.SetWellKnownGeogCS("EPSG:4326");
            } else {
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
            }
            geom.TransformTo(sr);
            double[] argout = new double[3];
            geom.GetPoint(0, argout);
            return new Position(argout[0], argout[1], argout[2]);
        }

        static public Geometry ToGeometry(this Vector3 position) {
            Geometry geom = new Geometry(wkbGeometryType.wkbPoint);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            geom.Vector3(new Vector3[1] { position });
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
            return point.ToGeometry().TransformWorld()[0];
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
            return position.ToGeometry(crs).TransformWorld()[0];
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
            return geom.TransformWorld();
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
        public static DCurve3 Vector3(this DCurve3 curve, Vector3[] verteces, bool bClosed)
        {
            curve.ClearVertices();
            curve.Closed = bClosed;
            foreach (Vector3 vertex in verteces)
            {
                curve.AppendVertex(vertex);
            }
            return curve;
        }

        public static DCurve3 FromGeometry(this DCurve3 curve, Geometry geom ) {
            if (geom.TransformTo(AppState.instance.mapProj) != 0)
                throw new NotSupportedException("projection failed");
            if (geom.Transform(AppState.instance.mapTrans) != 0)
                throw new NotSupportedException("axis change failed");
            int n = geom.GetPointCount();
            string crs;
            geom.GetSpatialReference().ExportToWkt(out crs, null);
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
                Vector3 local = (Vector3)vertexes[i];
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
        public static Vector3[] TransformWorld(this Geometry geom) {
            if (geom.GetCoordinateDimension() == 2) {
                geom.Set3D(1);
            };
            int dim = geom.GetCoordinateDimension();
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
                    Vector3 mapLocal =  (Vector3)new Vector3d(argout);
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
                    if (i!= 0 && !poly.Outer.Contains(vertex)) break;
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


    public static class MeshExtensions
    {
        /// <summary>
        /// Converts g3.DMesh3 to UnityEngine.Mesh
        /// </summary>
        /// <param name="dMesh">Dmesh3</param>
        /// <returns>UnityEngine.Mesh</returns>
        public static Mesh ToMesh(this DMesh3 dMesh)
        {
            Mesh unityMesh = new Mesh();
            DMesh3 mesh = new DMesh3();
            mesh.CompactCopy(dMesh);
            if (dMesh.HasMetadata) {
                string crs = dMesh.FindMetadata("CRS") as string;
                if (crs != null)
                    mesh.AttachMetadata("CRS", crs);
            }
            if ( ! mesh.Transform(AppState.instance.mapProj)) throw new Exception("Mesh Projjection Failed");
            Vector3[] vertices = new Vector3[mesh.VertexCount];
            Color[] colors = new Color[mesh.VertexCount];
            Vector2[] uvs = new Vector2[mesh.VertexCount];
            Vector3[] normals = new Vector3[mesh.VertexCount];
            NewVertexInfo data;
            for (int i = 0; i < dMesh.VertexCount; i++)
            {
                if (dMesh.IsVertex(i)) {
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
            if (mesh.HasVertexUVs) unityMesh.SetUVs(0,uvs);
            if (mesh.HasVertexNormals) unityMesh.SetNormals(normals);
            int[] triangles = new int[mesh.TriangleCount * 3];
            int j = 0;
            foreach (Index3i tri in mesh.Triangles())
            {
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
                } catch (Exception e) {
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
            } catch (Exception e) {
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
                boundsInFrame.Contain(frame.ToPlaneUV((Vector3f)bounds.Corner(i),3));
            }
            Vector2f min =(Vector2f)boundsInFrame.Min;
            float width = (float)boundsInFrame.Width;
            float height = (float)boundsInFrame.Height;

            for (int i = 0; i < dMesh.VertexCount; i++) {
                Vector2f UV = frame.ToPlaneUV((Vector3f) dMesh.GetVertex(i), 3);
                UV.x = (UV.x - min.x) / width;
                UV.y = (UV.y - min.y) / height;
                dMesh.SetVertexUV(i, UV);
            }
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


