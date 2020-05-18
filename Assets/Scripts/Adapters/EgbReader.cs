using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System;
using System.Text.RegularExpressions;
using OSGeo.OGR;
using GeoJSON.Net.Geometry;
using OSGeo.GDAL;
using OSGeo.OSR;
using GeoJSON.Net.Contrib.Wkb;
using GeoJSON.Net.CoordinateReferenceSystem;

namespace Virgis {

    public class EgbFeature {

        public EgbFeature() {
            image = new Dictionary<string, object>();
        }
        public string Id;
        public Dictionary<string, object> image;
        public LineString top;
        public LineString bottom;
    }

    public class EgbFeatureCollection : List<EgbFeature> {
    }

    /// <summary>
    /// Reader for .egb crossection definition files
    /// </summary>
    public class EgbReader  {
        static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))"; // Define delimiters, regular expression craziness
        static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r"; // Define line delimiters, regular experession craziness
        static char[] TRIM_CHARS = { '\"','/', '\\', ' ' };

        private string fileName;
        public string Payload;
        public EgbFeatureCollection features;

       SpatialReference EPSG4326 = new SpatialReference(
                @"GEOGCS[""WGS 84"",
                    DATUM[""WGS_1984"",
                        SPHEROID[""WGS 84"", 6378137, 298.257223563,
                            AUTHORITY[""EPSG"", ""7030""]],
                        AUTHORITY[""EPSG"", ""6326""]],
                    PRIMEM[""Greenwich"", 0,
                        AUTHORITY[""EPSG"", ""8901""]],
                    UNIT[""degree"", 0.0174532925199433,
                        AUTHORITY[""EPSG"", ""9122""]],
                    AUTHORITY[""EPSG"", ""4326""]]");
        SpatialReference EPSG32637 = new SpatialReference(
          @"PROJCS[""WGS 84 / UTM zone 37N"", GEOGCS[""WGS 84"",
                    DATUM[""WGS_1984"",
                        SPHEROID[""WGS 84"", 6378137, 298.257223563,
                        AUTHORITY[""EPSG"", ""7030""]],
                    AUTHORITY[""EPSG"", ""6326""]],
                PRIMEM[""Greenwich"", 0,
                AUTHORITY[""EPSG"", ""8901""]],
                UNIT[""degree"", 0.0174532925199433,
                AUTHORITY[""EPSG"", ""9122""]],
                AUTHORITY[""EPSG"", ""4326""]],
                PROJECTION[""Transverse_Mercator""],
                PARAMETER[""latitude_of_origin"", 0],
                PARAMETER[""central_meridian"", 39],
                PARAMETER[""scale_factor"", 0.9996],
                PARAMETER[""false_easting"", 500000],
                PARAMETER[""false_northing"", 0],
                UNIT[""metre"", 1,
                    AUTHORITY[""EPSG"", ""9001""]],
                AXIS[""Easting"", EAST],
                AXIS[""Northing"", NORTH],
                AUTHORITY[""EPSG"", ""32637""]]"");");

         CoordinateTransformation transform;

        public EgbReader() {
            try {
                GdalConfiguration.ConfigureOgr();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }

            transform = new CoordinateTransformation(EPSG32637, EPSG4326);
            

        }


        public async Task Load(string file) {
            fileName = file;
            char[] result;
            StringBuilder builder = new StringBuilder();
            try {
                using (StreamReader reader = File.OpenText(file)) {
                    result = new char[reader.BaseStream.Length];
                    await reader.ReadAsync(result, 0, (int) reader.BaseStream.Length);
                }

                foreach (char c in result) {
                    builder.Append(c);
                }
            } catch (Exception e) when (
                     e is UnauthorizedAccessException ||
                     e is DirectoryNotFoundException ||
                     e is FileNotFoundException ||
                     e is NotSupportedException
                     ) {
                Debug.LogError("Failed to Load" + file + " : " + e.ToString());
                Payload = null;
            }
            Payload = builder.ToString();
        }

        public void Read() {
            if (Payload != null) {
            
            string[] lines = Regex.Split(Payload, LINE_SPLIT_RE); // Split data.text into lines using LINE_SPLIT_RE characters

            if (lines.Length <= 1)
                return; //Check that there is more than one line

            bool imDef = false;
            bool obDef = false;

            EgbFeature feature = new EgbFeature();
            List<Position> top = new List<Position>();
            List<Position> bottom = new List<Position>();
            features = new EgbFeatureCollection();

                foreach (string line in lines) {
                    if (line.Contains("ImageDefinition Begin")) {
                        imDef = true;
                        obDef = false;
                        feature = new EgbFeature();
                    } else

                    if (line.Contains("ImageDefinition End")) {
                        imDef = false;
                        obDef = false;

                    } else

                    if (line.Contains("ObjectDefinition Begin")) {
                        imDef = false;
                        obDef = true;

                    } else

                    if (line.Contains("ObjectDefinition End")) {
                        feature.top = new LineString(top);
                        feature.bottom = new LineString(bottom);
                        features.Add(feature);
                        top = new List<Position>();
                        bottom = new List<Position>();
                        imDef = false;
                        obDef = false;

                    } else

                    if (imDef) {
                        string[] args = line.Split('=');
                        if (args[0].Contains("ID")) {
                            feature.Id = ParseValue(args[1]) as string;
                        } else {
                           feature.image.Add(ParseValue(args[0]) as string, ParseValue(args[1]));
                        }
                    } else

                    if (obDef) {
                        if (line.Contains("TopVertex")) {
                            top.Add(ParseGeometry(line));
                        } else
                        if (line.Contains("BottomVertex")) {
                            bottom.Add(ParseGeometry(line));
                        }
                    }
                }
            }
        }

        private Position  ParseGeometry(string line) {
            string[] args = line.Split('=');
            string[] coords = args[1].Split(',');
            if (coords.Length == 3) {
                try {
                    Geometry geom = new Geometry(wkbGeometryType.wkbPoint25D);
                    geom.AddPoint((double) ParseValue(coords[0]), (double) ParseValue(coords[1]), (double) ParseValue(coords[2]));
                    geom.Transform(transform);
                    Position ret = new Position(geom.GetY(0), geom.GetX(0), geom.GetZ(0));
                    return ret;
                } catch { 
                    return null; 
                }
            } else {
                Debug.Log("bbad Coords : " + line);
                return null;
            }
        }

        private object ParseValue(string value) {
            value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", ""); // Trim characters
            object finalvalue = value; //set final value

            double d; // Create double, to hold value if double

            if (double.TryParse(value, out d)) {
                finalvalue = d;
            }
            return finalvalue;
        }

    }
}