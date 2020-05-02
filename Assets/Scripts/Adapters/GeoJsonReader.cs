// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Project;
using System;

namespace Virgis
{


    public class GeoJsonReader
    {
        public string payload;
        public string fileName;

        public FeatureCollection getFeatureCollection()
        {
            if (payload is null)
            {
                return new FeatureCollection();
            }
            else
            {
                return JsonConvert.DeserializeObject<FeatureCollection>(payload);
            }
        }

        public async Task Load(string file)
        {
            fileName = file;
            char[] result;
            StringBuilder builder = new StringBuilder();
            try
            {
                using (StreamReader reader = File.OpenText(file))
                {
                    result = new char[reader.BaseStream.Length];
                    await reader.ReadAsync(result, 0, (int)reader.BaseStream.Length);
                }

                foreach (char c in result)
                {
                    builder.Append(c);
                }
            }
            catch (Exception e) when (
                   e is UnauthorizedAccessException ||
                   e is DirectoryNotFoundException ||
                   e is FileNotFoundException ||
                   e is NotSupportedException
                   )
            {
                Debug.LogError("Failed to Load" + file + " : " + e.ToString());
                payload = null;
            }
            payload = builder.ToString();
        }

        public GisProject GetProject()
        {
            return JsonConvert.DeserializeObject<GisProject>(payload);
        }

        public async Task Save()
        {
            using (StreamWriter writer = new StreamWriter(fileName, false))
            {
                await writer.WriteAsync(payload);
            }
        }

        public void SetFeatureCollection(FeatureCollection contents)
        {
            payload = JsonConvert.SerializeObject(contents, Formatting.Indented);
        }

        public void SetProject(GisProject project)
        {
            payload = JsonConvert.SerializeObject(project, Formatting.Indented);
        }
    }
}
