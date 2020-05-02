// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;

namespace Virgis
{

    public class Tools
    {

        static public Vector3 Ipos2Vect(Position position)
        {
            float Alt;
            if (position.Altitude == null) { Alt = 0.0f; } else { Alt = (float)position.Altitude * Global._map.WorldRelativeScale; };
            Vector3 _world = Conversions.GeoToWorldPosition(position.Latitude, position.Longitude, Global._map.CenterMercator, Global._map.WorldRelativeScale).ToVector3xz();
            _world.y = Alt;
            return _world;
        }

        static public Vector3[] LS2Vect(LineString line)
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
            Vector3 mapLocal = Global.Map.transform.InverseTransformPoint(position);
            Vector2d _latlng = VectorExtensions.GetGeoPosition(mapLocal, Global._map.CenterMercator, Global._map.WorldRelativeScale);
            return new Position(_latlng.x, _latlng.y, mapLocal.y / Global._map.WorldRelativeScale);
        }
    }
}
