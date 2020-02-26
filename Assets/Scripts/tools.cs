using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class Tools {

static public Vector3 Ipos2Vect(IPosition position, float y, AbstractMap _map) {
        Vector2d _latlon = new Vector2d(position.Latitude, position.Longitude);
        Vector2d _world = Conversions.GeoToWorldPosition(_latlon, _map.CenterMercator, _map.WorldRelativeScale);
        return  new Vector3((float)_world.x, y*_map.WorldRelativeScale, (float)_world.y); 
    }

static public Vector3[] LS2Vect(LineString line, float y, AbstractMap _map)
    {
        Vector3[] result = new Vector3[line.Coordinates.Count];
        for (int i = 0; i < line.Coordinates.Count; i++)
        {
            result[i] = Ipos2Vect(line.Coordinates[i], y, _map);
        }
        return result;
    }

}