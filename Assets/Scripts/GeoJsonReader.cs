// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Project;

public class GeoJsonReader
{
    public string payload;
    public string fileName;

    public FeatureCollection getFeatureCollection()
    {
        return JsonConvert.DeserializeObject<FeatureCollection>(payload);
    }

    public async Task Load(string file)
    {
        fileName = file;
        char[] result;
        StringBuilder builder = new StringBuilder();
        using (StreamReader reader = File.OpenText(file))
        {
            result = new char[reader.BaseStream.Length];
            await reader.ReadAsync(result, 0, (int)reader.BaseStream.Length);
        }

        foreach (char c in result)
        {
            builder.Append(c);
        }
        payload = builder.ToString();
    }

    public GisProject GetProject()
    {
        return JsonConvert.DeserializeObject<GisProject>(payload);
    }

    public async Task Save(FeatureCollection contents)
    {
        payload = JsonConvert.SerializeObject(contents, Formatting.Indented);
        using (StreamWriter writer = new StreamWriter(fileName, false))
        {
            await writer.WriteAsync(payload);
        }
    }
}
