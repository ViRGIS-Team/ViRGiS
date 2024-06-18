/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */
using VirgisGeometry;
using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using CoordinateTransformation = OSGeo.OSR.CoordinateTransformation;
using OSGeo.GDAL;
using System;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Linq;
using DXF = netDxf;
using Project;
using Unity.Jobs;
using Unity.Collections;

namespace Virgis {

    public static class DcurveExtensionsGeo {
        
        /// <summary>
        /// Creates ag3.DCurve in Map Space Coordinates from a geometry
        /// </summary>
        /// <param name="curve"> this curve</param>
        /// <param name="geom"> the OGR geometry to ue as ths source</param>
        /// <param name="crs"> the crs to u for the DCurve3 DEFAULT map default projections or project CRS if none</param>
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
            List<Vector3d> ls = new();
            double[] start = new double[3];
            for (int i = 0; i < n; i++) {
                double[] argout = new double[3];
                geom.GetPoint(i, argout);
                if (i == 0) {
                    start = argout;
                } else {
                    if (i + 1 == n && (
                        start[0] == argout[0] &&
                        start[1] == argout[1] &&
                        start[2] == argout[2]
                        )) {
                        // this is the end of a ring
                        curve.Closed = true;
                        break;
                    }
                }
                ls.Add(new Vector3d(argout));
            }
            curve.ClearVertices();
            curve.SetVertices(ls);
            return curve;
        }

        /// <summary>
        /// Converts DCurve3 in Local Vector3d coordinates to Vector3[] World coordinates 
        /// </summary>
        /// <param name="curve">input curve</param>
        /// <returns>Vector3[] in world coordinates</returns>
        public static Vector3[] ToWorld(this DCurve3 curve) {
            List<Vector3> ret = new List<Vector3>();
            List<Vector3d> vertexes = curve.Vertices as List<Vector3d>;
            for (int i = 0; i < curve.VertexCount; i++) {
                Vector3 local = (Vector3) vertexes[i];
                ret.Add(AppState.instance.Map.transform.TransformVector(local));
            }
            return ret.ToArray();
        }

        /// <summary>
        /// Calculates the 3D Centroid as a World space Vector3 of the DCurve3 that is in local map space.
        /// </summary>
        /// <param name="curve">DCurve3 in local map space coordinates</param>
        /// <returns>Vcetor3 in world space coordinates</returns>
        public static Vector3 WorldCenter(this DCurve3 curve) {
            return AppState.instance.Map.transform.TransformVector((Vector3) curve.Center());
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
                    ret.Add(AppState.instance.Map.transform.TransformPoint(mapLocal));
                }
            else {
                throw new NotSupportedException("no Points in geometry");
            }
            return ret.ToArray();
        }

        /// <summary>
        /// Converts World Space Vector3 positions to Points in the Geometry in Map Space coordinates
        /// </summary>
        /// <param name="geom"> Geometry top add the points to</param>
        /// <param name="points"> Array of Vector3 positions</param>
        /// <returns></returns>
        public static Geometry Vector3(this Geometry geom, Vector3[] points) {
            foreach (Vector3 point in points) {
                Vector3 mapLocal = AppState.instance.Map.transform.InverseTransformPoint(point);
                geom.AddPoint(mapLocal.x, mapLocal.z, mapLocal.y);
            }
            return geom;
        }
    }

    public static class MeshExtensionsGeo {

        /// <summary>
        /// Transform projected Dmesh to World Space
        /// </summary>
        /// <returns>bool true if successful</returns>
        public static bool Transform(this DMesh3 dMesh) {
            string crs = dMesh.FindMetadata("CRS") as string;
            // if the Dmesh3 contains a CRS use that
            if (crs != null && crs != "") {
                SpatialReference from = Convert.TextToSR(crs);
                CoordinateTransformation trans = AppState.instance.projectTransformer(from);
                return dMesh.Transform(trans);
            }
            return false;
        }

        public static bool Transform(this DMesh3 dMesh, CoordinateTransformation transformer) {
            try {
                for (int i = 0; i < dMesh.VertexCount; i++) {
                    if (dMesh.IsVertex(i)) {
                        Vector3d vertex = dMesh.GetVertex(i);
                        double[] dV = new double[3] { vertex.x, vertex.y, vertex.z };
                        transformer.TransformPoint(dV);
                        AppState.instance.mapTrans.TransformPoint(dV);
                        dMesh.SetVertex(i, new Vector3d(dV));
                    }
                };
                return true;
            } catch {
                return false;
            }
        }

        public static void CalculateMapUVs(this DMesh3 dMesh, Unit symbology) {
            if (symbology.TextureImage is not null && symbology.TextureImage != "") {
                dMesh.EnableVertexUVs(Vector2f.Zero);
                Dataset raster = Gdal.Open(symbology.TextureImage, Access.GA_ReadOnly);
                double[] gtRaw = new double[6];

                double X_size = raster.RasterXSize;
                double Y_size = raster.RasterYSize;

                raster.GetGeoTransform(gtRaw);
                if (gtRaw == null && gtRaw[1] == 0) {
                    throw new Exception();
                }

                NativeArray<double> geoTransform = new NativeArray<double>(gtRaw, Allocator.Persistent);
                NativeArray<double> U = new NativeArray<double>(dMesh.VertexCount, Allocator.Persistent);
                NativeArray<double> V = new NativeArray<double>(dMesh.VertexCount, Allocator.Persistent);

                NativeArray<Vector3d> vertices = new NativeArray<Vector3d>(dMesh.Vertices().ToArray<Vector3d>(), Allocator.Persistent);
                double F = geoTransform[2] / geoTransform[5];

                MapUV uv = new();
                uv.F0= 1/( (geoTransform[1] - F * geoTransform[4]) * X_size);
                uv.F1= F * uv.F0;
                uv.F2 = 1 / (geoTransform[5] * Y_size);
                uv.F3 = geoTransform[4] * uv.F2 * X_size;
                uv.vertices = vertices;
                uv.U= U;
                uv.V= V;
                uv.geoTransform= geoTransform;

                Stopwatch stopwatch= Stopwatch.StartNew();
                JobHandle jh = uv.Schedule(vertices.Length, 10);
                jh.Complete();
                Debug.Log($"uv Job took {stopwatch.Elapsed.TotalSeconds}");
                for (int i = 0; i < U.Length; i++) {
                    dMesh.SetVertexUV(i, new Vector2f((float)U[i], (float) V[i]));
                }
                Debug.Log($"uv total took {stopwatch.Elapsed.TotalSeconds}");

                geoTransform.Dispose();
                U.Dispose();
                V.Dispose();
                vertices.Dispose();
                
            } else {
                dMesh.CalculateUVs();
            }
        }

        public static Task<int> CalculateMapUVsAsync(this DMesh3 dMesh, Unit symbology) {

            TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
            Task<int> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {
                dMesh.CalculateMapUVs(symbology);
                tcs1.SetResult(1);
            });
            return t1;
        }

        [BurstCompile]
        struct MapUV : IJobParallelFor {
            [ReadOnly]
            public NativeArray<Vector3d> vertices;

            [ReadOnly]
            public NativeArray<double> geoTransform;

            [ReadOnly]
            public double F0;

            [ReadOnly]
            public double F1;

            [ReadOnly]
            public double F2;

            [ReadOnly]
            public double F3;

            public NativeArray<double> U;
            public NativeArray<double> V;

            public void Execute(int job) {
                Vector3d vertex = vertices[job];
                double X_geo = vertex.x - geoTransform[0];
                double Y_geo = vertex.y - geoTransform[3];

                double X = F0 * X_geo - F1 * Y_geo;
                double Y = F2 * Y_geo - F3 * X;

                U[job] = X;
                V[job] = 1 - Y;
            }
        }
    }

    public static class VirgisVectorExtensionsGeo {

        /// <summary>
        /// Convert vector3D in Y-up coordinate frame to a netDXF Vector3 in z-up coordinate frame
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
            for (int i = 0; i < fieldCount; i++) {
                FieldDefn fd = feature.GetFieldDefnRef(i);
                if (fd.GetName() == name) {
                    FieldType ft = fd.GetFieldType();
                    switch (ft) {
                        case FieldType.OFTString:
                            return feature.GetFieldAsString(i);
                        case FieldType.OFTReal:
                            return feature.GetFieldAsDouble(i);
                        case FieldType.OFTInteger:
                            return feature.GetFieldAsInteger(i);
                        case FieldType.OFTIntegerList:
                            break;
                        case FieldType.OFTRealList:
                            break;
                        case FieldType.OFTStringList:
                            break;
                        case FieldType.OFTWideString:
                            return feature.GetFieldAsString(i);
                        case FieldType.OFTWideStringList:
                            break;
                        case FieldType.OFTBinary:
                            break;
                        case FieldType.OFTDate:
                            break;
                        case FieldType.OFTTime:
                            break;
                        case FieldType.OFTDateTime:
                            break;
                        case FieldType.OFTInteger64:
                            return feature.GetFieldAsInteger(i);
                        case FieldType.OFTInteger64List:
                            break;
                        default:
                            return null;
                    }
                }
            }
            return null;
        }

        public static string GetString(this Feature feature, string name) {
            object value = feature.Get(name);
            try {
                return value.ToString();
            }
            catch (Exception e)
            {
                _ = e;
                return null;
                ;
            }
        }

        public static double GetDouble(this Feature feature, string name) {
            object value = feature.Get(name);
            try {
                return System.Convert.ToDouble(value);
            } catch (Exception e) {
                _ = e;
                return Double.Parse((string)value);
            }
        }

        public static int GetInt(this Feature feature, string name) {
            object value = feature.Get(name);
            try {
                return System.Convert.ToInt32(value);
            } catch (Exception e) {
                _ = e;
                return Int32.Parse((string) value);
            }
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
}