using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;

public struct MoveArgs {
    public int id;
    public Vector3 pos;
}


public struct GisProject
{
    [JsonProperty(PropertyName = "name")]
    public string Name;

    [JsonProperty(PropertyName = "origin")]
    public Point Origin;

    [JsonProperty(PropertyName = "zoom")]
    public int Zoom; 

    [JsonProperty(PropertyName = "layers")]
    public IList<Layer> Layers;
}

public struct Layer
{
    [JsonProperty(PropertyName = "type")]
    public string Type;
    [JsonProperty(PropertyName = "source")]
    public string Source;
}

