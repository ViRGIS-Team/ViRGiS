using System;
using Mapbox.Unity.Map;
using UnityEngine;
using Project;
using System.Collections;
using System.Collections.Generic;


public static class Global
{
    public static bool EditSession;
    public static AbstractMap _map;
    public static GameObject Map;
    public static GisProject project;
    public static List<GameObject> layers;
    public static GameObject mainCamera;
    public static GameObject trackingSpace;

    public static float WorldRelativeScale { get { return _map.WorldRelativeScale * Map.transform.lossyScale.sqrMagnitude / Vector3.one.magnitude; } }
}
