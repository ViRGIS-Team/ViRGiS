using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using Mapbox.Unity.Map;

public struct MoveArgs {
    public int id;
    public Vector3 pos;
    public Vector3 translate;
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

public static class Global
{
    public static bool EditSession;
    public static AbstractMap _map;
}

public static class PositionExtensionMethods
{
   public static Vector2 PointV2(this IPosition position)
    {
        return new Vector2((float)position.Latitude, (float)position.Longitude);
    }

    public static Position Point(this IPosition position)
    {
        return position as Position;
    }
}

public static class LineExtensionMethods
{
    public static Position Point(this LineString line, int i)
    {
        return line.Coordinates[i] as Position;
    }

    public static Position[] Points(this LineString line)
    {
        ReadOnlyCollection<IPosition> data = line.Coordinates;
        Position[] result = new Position[data.Count];
        for (int i=0; i<data.Count; i++)
        {
            result[i] = line.Point(i);
        }
        return result;
    }
}

