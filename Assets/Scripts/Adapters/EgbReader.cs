using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System;
using System.Text.RegularExpressions;




























































































































namespace Virgis {

    public class EgbFeature {
        Li
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
                return list; //Check that there is more than one line
        }

    }
}