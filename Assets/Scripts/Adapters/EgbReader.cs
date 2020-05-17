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



namespace Virgis {

    public class EgbFeature {
        public string Id;
        public Dictionary<string, object> Image;
        public LineString top;
        public LineString bottom;
    }

    /// <summary>
    /// Reader for .egb crossection definition files
    /// </summary>
    public class EgbReader : MonoBehaviour {
        static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))"; // Define delimiters, regular expression craziness
        static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r"; // Define line delimiters, regular experession craziness
        static char[] TRIM_CHARS = { '\"' };

        private string fileName;
        public string Payload;
        public List<EgbFeature> features;


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
            var lines = Regex.Split(Payload, LINE_SPLIT_RE); // Split data.text into lines using LINE_SPLIT_RE characters

            if (lines.Length <= 1)
                return; //Check that there is more than one line

            bool imDef = false;
            bool obDef = false;

            EgbFeature feature = new EgbFeature();
            List<Position> top = new List<Position>();
            List<Position> bottom = new List<Position>();

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
                    imDef = false;
                    obDef = false;

                } else 

                if (imDef) {
                    string[] args = line.Split('=');
                    if (args[0].Contains("ID")) {
                        feature.Id = args[1].Trim().Trim('"');
                    } else {
                        feature.Image.Add(args[0].Trim(), args[1].Trim().Trim('"'));
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

        private Position ParseGeometry(string line) {
            string[] args = line.Split('=');
            string[] coords = line.Split(',');
            if (coords.Length == 3) {
                try {
                    return  new Position(Convert.ToDouble(coords[0]), Convert.ToDouble(coords[0]), Convert.ToDouble(coords[0]));
                } catch { return null; }
            } else {
                Debug.Log("bbad Coords : " + line);
                return null;
            }
        }

    }
}