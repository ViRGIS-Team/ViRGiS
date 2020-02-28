// copyright Runette Software Ltd, 2020. All rights reserved
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

static public Vector3 Ipos2Vect(Position position) {
        float Alt;
        if (position.Altitude == null) { Alt = 0.0f; } else { Alt = (float)position.Altitude * Global._map.WorldRelativeScale; } ;
        Vector2 _latlng = position.PointV2();
        Vector3 _world = VectorExtensions.AsUnityPosition(_latlng, Global._map.CenterMercator, Global._map.WorldRelativeScale);
        _world.y = Alt;
        return _world;
    }

static public Vector3[] LS2Vect(LineString line, AbstractMap _map)
    {
        Vector3[] result = new Vector3[line.Coordinates.Count];
        for (int i = 0; i < line.Coordinates.Count; i++)
        {
            result[i] = Ipos2Vect(line.Point(i));
        }
        return result;
    }

static public IPosition Vect2Ipos(Vector3 position)
    {
        Vector2d _latlng = VectorExtensions.GetGeoPosition(position, Global._map.CenterMercator, Global._map.WorldRelativeScale);
        return new Position(_latlng.x, _latlng.y, position.y / Global._map.WorldRelativeScale);
    }

}
