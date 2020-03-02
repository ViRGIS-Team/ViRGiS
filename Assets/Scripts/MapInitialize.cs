// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using GeoJSON.Net.Geometry;
using Project;

public class MapInitialize : MonoBehaviour
{

    public float startAltitude = 50f;
    public GameObject PointLayer;
    public GameObject LineLayer;
    public GameObject PolygonLayer;
    public GameObject PointCloud;
    public GameObject MeshLayer;

    public string inputfile;
    // Start is called before the first frame update

    //Events
    public EventManager eventManager;

    async void Start()
    {

        eventManager = gameObject.AddComponent<EventManager>();

        // Fetch Project definition
        GeoJsonReader geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        GisProject project = geoJsonReader.GetProject();

        Vector2d origin = project.Origin.Coordinates.Vector2d();

        //initialize space
        AbstractMap _map = GetComponent<AbstractMap>();
        _map.Initialize(origin, project.MapScale);

        //set globals
        Global._map = _map;
        Global.EditSession = false;
        GameObject Map = gameObject;
        Global.Map = Map;
        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.position = project.Camera.Coordinates.Vector3();

        //load the layers
        foreach (RecordSet layer in project.RecordSets)
        {
            if (layer.DataType == RecordSetDataType.Point)
            {
                GameObject temp = await Instantiate(PointLayer, Vector3.zero, Quaternion.identity).GetComponent<PointLayer>().Init(layer.Source);
                temp.transform.parent = Map.transform;
            }
            else if (layer.DataType == RecordSetDataType.Line)
            {
                GameObject temp = await  Instantiate(LineLayer, Vector3.zero, Quaternion.identity).GetComponent<LineLayer>().Init(layer.Source);
                temp.transform.parent = Map.transform;
            }
            else if (layer.DataType == RecordSetDataType.Polygon)
            {
                GameObject temp = await  Instantiate(PolygonLayer, Vector3.zero, Quaternion.identity).GetComponent<PolygonLayer>().Init(layer.Source);
                temp.transform.parent = Map.transform;
            }
            else if (layer.DataType == RecordSetDataType.PointCloud)
            {
                GameObject temp = await Instantiate(PointCloud, layer.Position.Coordinates.Vector3(), Quaternion.identity).GetComponent<PointCloudExporter.PointCloudGenerator>().Init(layer.Source, layer.Transform.Rotate, layer.Transform.Scale, (Vector3)layer.Transform.Position  * Global._map.WorldRelativeScale );
                temp.transform.parent = Map.transform;
            }
            else if (layer.DataType == RecordSetDataType.Mesh)
            {
                GameObject temp = await Instantiate(MeshLayer, layer.Position.Coordinates.Vector3(), Quaternion.identity).GetComponent<ObjLoader>().Init(layer.Source, layer.Transform.Rotate, layer.Transform.Scale, (Vector3)layer.Transform.Position * Global._map.WorldRelativeScale);
                temp.transform.parent = Map.transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
