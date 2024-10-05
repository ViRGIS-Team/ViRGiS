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

using CRSType = GeoJSON.Net.CoordinateReferenceSystem.CRSType;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Geometry;
using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using VirgisGeometry;
using OSGeo.OSR;


namespace Virgis {

    public static class Vector3ExtensionMethods {
        /// <summary>
        /// Convert Vector3d Map Space location to Position in stated CRS or epsg:4326 by default
        /// </summary>
        /// <param name="position">Vector3 Map Space coordinates</param>
        /// <param name="crs">ICRSObject to use for projection</param>
        /// <returns>Position</returns>
        static public IPosition ToPosition(this Vector3d position, ICRSObject crs = null) {
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
            double[] args = new[] {position.x, position.y, position.z};

            AppState.instance
                .projectOutTransformer(sr)
                .TransformPoint(args);
            return new Position(args[0], args[1], args[2]);
        }

        /// <summary>
        /// Converts a Vector3d position in Map Space coordinates into a valid Geometry with CRS set
        /// (the CRS being the model engineering projection - Geometry.transform to change that)
        /// <param name="position">Vector3 position in World Space Coordinates</param>
        /// <returns>Geometry</returns>
        static public Geometry ToGeometry(this Vector3d position) {
            Geometry geom = new Geometry(wkbGeometryType.wkbPoint);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            geom.AddPoint(position.x,position.y, position.z );
            return geom;
        }

        /// <summary>
        /// Converts Vector3d Map Space Location to Point
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Point ToPoint(this Vector3d point) {
            return new Point(point.ToPosition());
        }
    }

    public static class PointExtensionsMethods {
        static public Geometry ToGeometry(this Point point) {
            return point.Coordinates.ToGeometry(point.CRS);
        }


        /// <summary>
        /// Convert Point (with arbitrary CRS) to Vector3d in Map Space
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        static public Vector3d ToVector3d(this Point point) {
            Geometry geom = point.ToGeometry();
            SpatialReference sr = geom.GetSpatialReference();
            geom.Transform(
                AppState.instance.projectTransformer(sr)
                );
            double[] argouts = new double[3];
            geom.GetPoint(0,argouts);
            return new Vector3d(argouts) { axisOrder = sr.GetAxisOrder() };
        }
    }

    public static class PositionExtensionMethods {

        /// <summary>
        /// Converts IPosition to UnityEngine.vector2
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>UnityEngine.Vector2</returns>
        public static Vector2 Vector2(this IPosition position) {
            return new Vector2((float) position.Latitude, (float) position.Longitude);
        }

        public static Geometry ToGeometry(this IPosition position, ICRSObject crs = null) {
            Geometry geom = new Geometry(wkbGeometryType.wkbPoint);
            SpatialReference sr = new SpatialReference(null);
            if (crs is null)
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
            geom.AddPoint(position.Latitude, position.Longitude, alt ?? 0.0);
            return geom;
        }
    }

    public static class LineExtensionMethods {
        /// <summary>
        /// Converts LineString Vertex i to a Position
        /// </summary>
        /// <param name="line">LineString</param>
        /// <param name="i">vertex index</param>
        /// <returns>Position</returns>
        public static Position Point(this LineString line, int i) {
            return line.Coordinates[i] as Position;
        }

        /// <summary>
        /// Converts LineString to Position[]
        /// </summary>
        /// <param name="line">LineString</param>
        /// <returns>Position[]</returns>
        public static Position[] Points(this LineString line) {
            ReadOnlyCollection<IPosition> data = line.Coordinates;
            Position[] result = new Position[data.Count];
            for (int i = 0; i < data.Count; i++) {
                result[i] = line.Point(i);
            }
            return result;
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
}
