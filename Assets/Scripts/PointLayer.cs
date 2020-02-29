// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;

public class PointLayer : MonoBehaviour
{
    // The prefab for the data points to be instantiated
    public GameObject PointPrefab;

    private GeoJsonReader geoJsonReader;

    private void Start()
    {
        StartCoroutine(GetEvents());
    }

    public async Task<GameObject> Init(string inputfile)
    {
        // get geojson data
        AbstractMap _map = Global._map;

        geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();

        foreach (Feature feature in myFC.Features)
        {
            // Get the geometry
            Point geometry = feature.Geometry as Point;
            IDictionary<string, object> properties = feature.Properties;
            string gisId = feature.Id;
            string name = (string)properties["name"];
            string type = (string)properties["type"];
            Position in_position = geometry.Coordinates as Position;
            Vector2d _location = new Vector2d(in_position.Latitude, in_position.Longitude);

            //float y = (float)in_position.Altitude;
            float y = _map.QueryElevationInMetersAt(_location);

            //instantiate the prefab with coordinates defined above
            GameObject dataPoint = Instantiate(PointPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            dataPoint.transform.parent = gameObject.transform;
            // add the gis data from geoJSON
            DatapointSphere com = dataPoint.GetComponent<DatapointSphere>();
            com.gisId = gisId;
            com.gisProperties = properties;
            GameObject labelObject = new GameObject();
            labelObject.transform.parent = dataPoint.transform;
            labelObject.transform.localRotation = Quaternion.Euler(0,180,0);
            TextMesh labelMesh = labelObject.AddComponent(typeof(TextMesh)) as TextMesh;
            labelMesh.text = name + "," + type;
            //Set the color
            dataPoint.SendMessage("SetColor", Color.blue);
            Vector3 scaleChange = new Vector3(1, 1, 1);
            dataPoint.transform.localScale = scaleChange;
            Vector2d pos = Conversions.GeoToWorldPosition(_location, _map.CenterMercator, _map.WorldRelativeScale);
            dataPoint.transform.position = new Vector3((float)pos.x, y * _map.WorldRelativeScale, (float)pos.y);
        };
        return gameObject;
    }

    public void ExitEditsession()
    {
        Save();
    }

    public async void Save()
    {
        DatapointSphere[] pointFuncs = gameObject.GetComponentsInChildren<DatapointSphere>();
        List<Feature> features = new List<Feature>();
        foreach (DatapointSphere pointFunc in pointFuncs)
        {
            features.Add( new Feature(new Point(Tools.Vect2Ipos(pointFunc.gameObject.transform.position)),pointFunc.gisProperties, pointFunc.gisId));
        }
        FeatureCollection FC = new FeatureCollection(features);
        await geoJsonReader.Save(FC);

    }

    IEnumerator GetEvents()
    {
        Camera camera = Camera.main;
        EventManager eventManager;
        do
        {
            eventManager = camera.gameObject.GetComponent<EventManager>();
            if (eventManager == null) { new WaitForSeconds(.5f); };
        } while (eventManager == null);
        eventManager.OnEditsessionEnd.AddListener(ExitEditsession);
        yield return eventManager;
    }

}
