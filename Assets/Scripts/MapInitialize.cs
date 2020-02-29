// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using GeoJSON.Net.Geometry;

public class MapInitialize : MonoBehaviour
{

    public float startAltitude = 50f;
    public GameObject PointLayer;
    public GameObject LineLayer;
    public GameObject PolygonLayer;
    public string inputfile;
    // Start is called before the first frame update
    async void Start()
    {
        // Fetch Project definition
        GeoJsonReader geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        GisProject project = geoJsonReader.GetProject();

        Vector2d origin = new Vector2d(project.Origin.Coordinates.Latitude, project.Origin.Coordinates.Longitude);

        //initialize space
        AbstractMap _map = GetComponent<AbstractMap>();
        _map.Initialize(origin, project.Zoom);

        //set globals
        Global._map = _map;
        Global.EditSession = false;
        GameObject Map = _map.gameObject;
        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.position = Tools.Ipos2Vect(project.Camera.Coordinates as Position);

        //load the layers
        foreach (Layer layer in project.Layers)
        {
            if (layer.Type == "Point")
            {
                GameObject temp = await Instantiate(PointLayer, Vector3.zero, Quaternion.identity).GetComponent<PointLayer>().Init(layer.Source);
                temp.transform.parent = Map.transform;
            }
            else if (layer.Type == "Line")
            {
                GameObject temp = await  Instantiate(LineLayer, Vector3.zero, Quaternion.identity).GetComponent<LineLayer>().Init(layer.Source);
                temp.transform.parent = Map.transform;
            }
            else if (layer.Type == "Polygon")
            {
                GameObject temp = await  Instantiate(PolygonLayer, Vector3.zero, Quaternion.identity).GetComponent<PolygonLayer>().Init(layer.Source);
                temp.transform.parent = Map.transform;
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
