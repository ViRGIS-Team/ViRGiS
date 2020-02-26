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
    // Start is called before the first frame update
    void Start()
    {
        AbstractMap _map = GetComponent<AbstractMap>();
        _map.Initialize(new Vector2d(51.282433, 1.379470), 15);

        Instantiate(PointLayer, Vector3.zero, Quaternion.identity).GetComponent<DataPlotterJson>().Init(_map);
        Instantiate(LineLayer, Vector3.zero, Quaternion.identity).GetComponent<LineLayer>().Init(_map);
        Instantiate(PolygonLayer, Vector3.zero, Quaternion.identity).GetComponent<PolygonLayer>().Init(_map);

        Vector2d origin = _map.CenterLatitudeLongitude;
        float originElevation = _map.QueryElevationInMetersAt(origin);
        GameObject camera = GameObject.Find("Main Camera");
        camera.transform.position = new Vector3(0, (originElevation + startAltitude) * _map.WorldRelativeScale, 0);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
