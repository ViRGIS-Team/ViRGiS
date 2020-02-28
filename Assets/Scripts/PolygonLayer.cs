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

public class PolygonLayer : MonoBehaviour
{

    // Name of the input file, no extension
    public string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;
    public GameObject HandlePrefab;
    public GameObject PolygonPrefab;
    public Material Mat;

    private GeoJsonReader geoJsonReader;

    private void Start()
    {
        StartCoroutine(GetEvents());
    }


    public async Task Init(string source)
    {
        // get geojson data
        AbstractMap _map = Global._map;
        inputfile = source;

        geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();

        foreach (Feature feature in myFC.Features)
        {
            Polygon geometry = feature.Geometry as Polygon;
            IDictionary<string, object> properties = feature.Properties;
            string name = (string)properties["name"];
            string type = (string)properties["type"];
            string gisId = feature.Id;
            ReadOnlyCollection<LineString> LinearRings = geometry.Coordinates;
            LineString perimeter = LinearRings[0];
            GameObject dataLine = Instantiate(LinePrefab, Tools.Ipos2Vect(perimeter.Coordinates[0] as Position), Quaternion.identity);
            dataLine.GetComponent<DatalineCylinder>().Draw(perimeter, Color.red, 0.5f, LinePrefab, HandlePrefab, _map);
            //dataLine.GetComponentInChildren<TextMesh>().text = name + "," + type;
            Vector3[] poly = Tools.LS2Vect(perimeter, _map);
            Vector3 center = Poly.FindCenter(poly);
            GameObject dataPoly = Instantiate(PolygonPrefab, center, Quaternion.identity);
            Datapolygon com = dataPoly.GetComponent<Datapolygon>();
            com.gisId = gisId;
            com.gisProperties = properties;
            dataPoly.transform.parent = gameObject.transform;
            dataLine.transform.parent = dataPoly.transform;
            Poly.Draw(poly, center, dataPoly, Mat);
        };

    }

    public void ExitEditsession()
    {
        Save();
    }

    public async void Save()
    {
        Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
        List<Feature> features = new List<Feature>();
        foreach (Datapolygon dataFeature in dataFeatures)
        {
            Debug.Log(dataFeature.ToString());
            DatalineCylinder perimeter = dataFeature.GetComponentInChildren<DatalineCylinder>();
            Vector3[] vertices = perimeter.GetVertices();
            List<Position> positions = new List<Position>();
            foreach (Vector3 vertex in vertices)
            {
                positions.Add(Tools.Vect2Ipos(vertex) as Position);
            }
            LineString line = new LineString(positions);
            if (!line.IsLinearRing())
            {
                Debug.LogError("This Polygon is not a Linear Ring");
                throw new System.ArgumentException("This Polygon is not a Linear Ring", dataFeature.gisProperties.ToString());
            }
            List<LineString> LinearRings = new List<LineString>();
            LinearRings.Add(line);
            features.Add(new Feature(new Polygon(LinearRings), dataFeature.gisProperties, dataFeature.gisId));
        };
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
