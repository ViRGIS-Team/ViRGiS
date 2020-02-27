using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class GeoJsonReader 
{
    public string payload;

    //public void Load(string file){
    //    payload = Resources.Load(file) as TextAsset;
    //    //Debug.Log(payload);
    //}
    public FeatureCollection getFeatureCollection() {
        return JsonConvert.DeserializeObject<FeatureCollection>(payload);  
    }

    public async Task Load(string file)
    {
        Debug.Log("hello");
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
        Debug.Log(payload);
    }

    public GisProject GetProject()
    {
        return JsonConvert.DeserializeObject<GisProject>(payload);
    }
}
