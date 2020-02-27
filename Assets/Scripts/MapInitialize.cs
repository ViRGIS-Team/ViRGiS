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

        GeoJsonReader geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        GisProject project = geoJsonReader.GetProject();

        Vector2d origin = new Vector2d(project.Origin.Coordinates.Latitude, project.Origin.Coordinates.Longitude);

        AbstractMap _map = GetComponent<AbstractMap>();
        _map.Initialize(origin, project.Zoom);

        foreach (Layer layer in project.Layers)
        {
            if (layer.Type == "Point")
            {
                Instantiate(PointLayer, Vector3.zero, Quaternion.identity).GetComponent<DataPlotterJson>().Init(_map, layer.Source);
            }
            else if (layer.Type == "Line")
            {
                Instantiate(LineLayer, Vector3.zero, Quaternion.identity).GetComponent<LineLayer>().Init(_map, layer.Source );
            }
            else if (layer.Type == "Polygon")
            {
                Instantiate(PolygonLayer, Vector3.zero, Quaternion.identity).GetComponent<PolygonLayer>().Init(_map, layer.Source);
            }
        }

        float originElevation = _map.QueryElevationInMetersAt(origin);
        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.position = new Vector3(0, (originElevation + startAltitude) * _map.WorldRelativeScale, 0);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
