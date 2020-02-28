using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;

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
        float originElevation = _map.QueryElevationInMetersAt(origin);
        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.position = new Vector3(0, (originElevation + startAltitude) * _map.WorldRelativeScale, 0);

        //set globals
        Global._map = _map;
        Global.EditSession = false;

        //load the layers
        foreach (Layer layer in project.Layers)
        {
            if (layer.Type == "Point")
            {
                _ = Instantiate(PointLayer, Vector3.zero, Quaternion.identity).GetComponent<PointLayer>().Init(layer.Source);
            }
            else if (layer.Type == "Line")
            {
                _ = Instantiate(LineLayer, Vector3.zero, Quaternion.identity).GetComponent<LineLayer>().Init(layer.Source);
            }
            else if (layer.Type == "Polygon")
            {
                _ = Instantiate(PolygonLayer, Vector3.zero, Quaternion.identity).GetComponent<PolygonLayer>().Init(layer.Source);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
