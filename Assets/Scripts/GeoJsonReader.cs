using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;

public class GeoJsonReader 
{
    public TextAsset payload;

    public void Load(string file){
        payload = Resources.Load(file) as TextAsset;
        //Debug.Log(payload);
    }
    public FeatureCollection getFeatureCollection() {
        return JsonConvert.DeserializeObject<FeatureCollection>(payload.text);  
    }
}
