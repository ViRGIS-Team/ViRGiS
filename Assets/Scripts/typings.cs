// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Mapbox.Utils;


public struct MoveArgs {
    public int id;
    public Vector3 pos;
    public Vector3 translate;
    public Vector3 oldPos;
}

public static class PositionExtensionMethods
{
   public static Vector2d Vector2d(this IPosition position)
    {
        return new Vector2d((float)position.Latitude, (float)position.Longitude);
    }

    public static Vector2 Vector2(this IPosition position)
    {
        return new Vector2((float)position.Latitude, (float)position.Longitude);
    }

    public static Position Point(this IPosition position)
    {
        return position as Position;
    }

    public static Vector3 Vector3(this IPosition position)
    {
        return Tools.Ipos2Vect(position as Position);
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

public class TestableObject
{
    public bool ContainsKey( string propName)
    {
        return GetType().GetMember(propName) != null;
    }
}
