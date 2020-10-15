using CRSType = GeoJSON.Net.CoordinateReferenceSystem.CRSType;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Geometry;
using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using System;
using System.Collections.ObjectModel;
using UnityEngine;


namespace Virgis {

    public static class Vector3ExtensionMethods {
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

    public static class PositionExtensionMethods {
        /// <summary>
        /// Converts Iposition to Vector2D
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>Mapbox.Utils.Vector2d</returns>
        public static Mapbox.Utils.Vector2d Vector2d(this IPosition position) {
            return new Mapbox.Utils.Vector2d(position.Latitude, position.Longitude);
        }

        /// <summary>
        /// Converts IPosition to UnityEngine.vector2
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>UnityEngine.Vector2</returns>
        public static Vector2 Vector2(this IPosition position) {
            return new Vector2((float) position.Latitude, (float) position.Longitude);
        }


        /// <summary>
        /// Converts Iposition to Vector3 World Space coordinates takling account of zoom, scale and mapscale
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>Vector3</returns>
        public static Vector3 Vector3(this IPosition position, ICRSObject crs = null) {
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
}
