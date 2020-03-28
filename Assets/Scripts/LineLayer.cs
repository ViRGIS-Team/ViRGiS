// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;
using Project;

public class LineLayer : MonoBehaviour, ILayer
{
    // Name of the input file, no extension
    public string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;
    public GameObject HandlePrefab;

    private GeoJsonReader geoJsonReader;

    public bool changed { get; set; }
    public RecordSet layer { get; set; }

    private void Start()
    {
        StartCoroutine(GetEvents());
    }


    // Start is called before the first frame update
    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;
        // get geojson data
        AbstractMap _map = Global._map;
        inputfile = layer.Source;
        Dictionary<string, Unit> symbology = layer.Properties.Units;

        geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();


        foreach (Feature feature in myFC.Features)
        {
            // Get the geometry
            MultiLineString geometry = feature.Geometry as MultiLineString;
            IDictionary<string, object> properties = feature.Properties;
            string gisId = feature.Id;

            //create the GameObjects
            ReadOnlyCollection<LineString> lines = geometry.Coordinates;
            GameObject dataLine = Instantiate(LinePrefab, Tools.Ipos2Vect(lines[0].Point(0)), Quaternion.identity);
            dataLine.transform.parent = gameObject.transform;

            //set the gisProject properties
            DatalineCylinder com = dataLine.GetComponent<DatalineCylinder>();
            com.gisId = gisId;
            com.gisProperties = properties;

            //Draw the line
            com.Draw(lines[0], symbology["default"], LinePrefab, HandlePrefab, _map);
            //dataLine.GetComponentInChildren<TextMesh>().text = name + "," + type;

        };
        changed = false;
        return gameObject;
    }

    public void ExitEditsession()
    {
        Save();
    }

    public async void Save()
    {
        DatalineCylinder[] dataFeatures = gameObject.GetComponentsInChildren<DatalineCylinder>();
        List<Feature> features = new List<Feature>();
        foreach (DatalineCylinder dataFeature in dataFeatures)
        {
            Vector3[] vertices = dataFeature.GetVertices();
            List<Position> positions = new List<Position>();
            foreach (Vector3 vertex in vertices)
            {
                positions.Add(Tools.Vect2Ipos(vertex) as Position);
            }
            List<LineString> lines = new List<LineString>();
            lines.Add(new LineString(positions));
            features.Add(new Feature(new MultiLineString(lines), dataFeature.gisProperties, dataFeature.gisId));
        };
        FeatureCollection FC = new FeatureCollection(features);
        geoJsonReader.SetFeatureCollection(FC);
        await geoJsonReader.Save();
    }

    IEnumerator GetEvents()
    {
        GameObject Map = Global.Map;
        EventManager eventManager;
        do
        {
            eventManager = Map.GetComponent<EventManager>();
            if (eventManager == null) { new WaitForSeconds(.5f); };
        } while (eventManager == null);
        eventManager.OnEditsessionEnd.AddListener(ExitEditsession);
        yield return eventManager;
    }

}
