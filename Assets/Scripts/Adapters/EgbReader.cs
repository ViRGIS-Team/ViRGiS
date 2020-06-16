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


namespace Virgis {

    public class EgbFeature {

        public EgbFeature() {
            image = new Dictionary<string, object>();
        }
        public string Id;
        public Dictionary<string, object> image;
        public Geometry top;
        public Geometry bottom;
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
        public SpatialReference CRS;

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


         CoordinateTransformation transform;

        public EgbReader() {
            try {
                GdalConfiguration.ConfigureOgr();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }
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
            Geometry top = new Geometry(wkbGeometryType.wkbLineString25D);
            Geometry bottom = new Geometry(wkbGeometryType.wkbLineString25D);
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
                    feature.top = top;
                    feature.bottom = bottom;
                    features.Add(feature);
                    top = new Geometry(wkbGeometryType.wkbLineString25D);
                    bottom = new Geometry(wkbGeometryType.wkbLineString25D);
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
                        ParseGeometry(line, top);
                    } else
                    if (line.Contains("BottomVertex")) {
                        ParseGeometry(line, bottom);
                    }
                } else

                if (line.Contains("CoordinateSystem")) {
                    string[] args = line.Split('=');
                    string coordsys = ParseValue(args[1]) as string;
                    CRS = new SpatialReference(null);
                    CRS.ImportFromMICoordSys(coordsys);
                    transform = new CoordinateTransformation(CRS, EPSG4326);
                }
            }

            foreach (EgbFeature f in features) {
                f.top.Transform(transform);
                f.bottom.Transform(transform);
            }
        }
    }

        private void  ParseGeometry(string line, Geometry geom ) {
            string[] args = line.Split('=');
            string[] coords = args[1].Split(',');
            if (coords.Length == 3) {
                geom.AddPoint((double) ParseValue(coords[0]), (double) ParseValue(coords[1]), (double) ParseValue(coords[2]));
            } else {
                Debug.Log("bad Coords : " + line);
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