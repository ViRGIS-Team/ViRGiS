using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System;

// Taken from here: https://bravenewmethod.com/2014/09/13/lightweight-csv-reader-for-unity/
// Comments

// Code parses a CSV, converting values into ints or floats if able, and returning a List<Dictionary<string, object>>.

namespace Virgis {

    public class CSVRow : Dictionary<string, object> {}
    public class CSVData : List<CSVRow> {}
    public class CSVReader {
        static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))"; // Define delimiters, regular expression craziness
        static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r"; // Define line delimiters, regular experession craziness
        static char[] TRIM_CHARS = { '\"' };

        private string fileName;
        public string Payload;

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

        public CSVData Read() {
            var list = new CSVData(); //declare dictionary listion

            var lines = Regex.Split(Payload, LINE_SPLIT_RE); // Split data.text into lines using LINE_SPLIT_RE characters

            if (lines.Length <= 1)
                return list; //Check that there is more than one line

            var header = Regex.Split(lines[0], SPLIT_RE); //Split header (element 0)

            // Loops through lines
            for (var i = 1; i < lines.Length; i++) {

                var values = Regex.Split(lines[i], SPLIT_RE); //Split lines according to SPLIT_RE, store in var (usually string array)
                if (values.Length == 0 || values[0] == "")
                    continue; // Skip to end of loop (continue) if value is 0 length OR first value is empty

                var entry = new CSVRow(); // Creates dictionary object

                // Loops through every value
                for (var j = 0; j < header.Length && j < values.Length; j++) {
                    string value = values[j]; // Set local variable value
                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", ""); // Trim characters
                    object finalvalue = value; //set final value

                    int n; // Create int, to hold value if int

                    float f; // Create float, to hold value if float

                    // If-else to attempt to parse value into int or float
                    if (int.TryParse(value, out n)) {
                        finalvalue = n;
                    } else if (int.TryParse(value, System.Globalization.NumberStyles.HexNumber, new CultureInfo("en-US"), out n)) {
                        finalvalue = n;
                    } else if (float.TryParse(value, out f)) {
                        finalvalue = f;
                    }
                    entry[header[j]] = finalvalue;
                }
                list.Add(entry); // Add Dictionary ("entry" variable) to list
            }
            return list; //Return list
        }
    }
}