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
using Newtonsoft.Json.Linq;

public class PolygonLayer : MonoBehaviour, Layer
{

    // Name of the input file, no extension
    public string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;
    public GameObject HandlePrefab;
    public GameObject PolygonPrefab;
    public Material Mat;

    private GeoJsonReader geoJsonReader;
    public RecordSet layer { get; set; }
    public bool changed { get; set; }

    private void Start()
    {
        StartCoroutine(GetEvents());
    }


    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;

        // get geojson data
        AbstractMap _map = Global._map;
        inputfile = layer.Source;
        Dictionary<string, Unit> symbology = layer.Properties.Units;
        //Material Mat = new Material(Shader.Find("PDT Shaders/TestGrid"));

        geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(inputfile);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();

        foreach (Feature feature in myFC.Features)
        {
            Polygon geometry = feature.Geometry as Polygon;
            IDictionary<string, object> properties = feature.Properties;
            string gisId = feature.Id;
            ReadOnlyCollection<LineString> LinearRings = geometry.Coordinates;
            LineString perimeter = LinearRings[0];
            Vector3[] poly = Tools.LS2Vect(perimeter, _map);
            Vector3 center = Vector3.zero;
            if (properties.ContainsKey("polyhedral") && properties["polyhedral"] != null)
            {
                JObject jobject = (JObject)properties["polyhedral"];
                Point centerPoint = jobject.ToObject<Point>();
                center = centerPoint.Coordinates.Vector3();
                properties["polyhedral"] = new Point(Tools.Vect2Ipos(center));
            }
            else
            {
                center = Datapolygon.FindCenter(poly);
                properties["polyhedral"] = new Point(Tools.Vect2Ipos(center));
            }

            //Create the GameObjects
            GameObject dataLine = Instantiate(LinePrefab, Tools.Ipos2Vect(perimeter.Coordinates[0] as Position), Quaternion.identity);
            GameObject dataPoly = Instantiate(PolygonPrefab, center, Quaternion.identity);
            GameObject centroid = Instantiate(HandlePrefab, center, Quaternion.identity);
            dataPoly.transform.parent = gameObject.transform;
            dataLine.transform.parent = dataPoly.transform;
            centroid.transform.parent = dataPoly.transform;

            // add the gis data from geoJSON
            Datapolygon com = dataPoly.GetComponent<Datapolygon>();
            com.gisId = gisId;
            com.gisProperties = properties;
            com.centroid = centroid.GetComponent<DatapointSphere>();

            //Draw the Polygon
            Mat.SetColor("_BaseColor", symbology["body"].Color);
            com.Draw(poly, Mat);
            dataLine.GetComponent<DatalineCylinder>().Draw(perimeter, symbology["line"], LinePrefab, HandlePrefab, _map);
            centroid.SendMessage("SetColor", (Color)symbology["line"].Color);
            centroid.SendMessage("SetId", -1);
            centroid.transform.localScale = symbology["line"].Transform.Scale;
            centroid.transform.localRotation = symbology["line"].Transform.Rotate;
            centroid.transform.localPosition = symbology["line"].Transform.Position;


            //Set the label
            GameObject labelObject = new GameObject();
            labelObject.transform.parent = centroid.transform;
            labelObject.transform.localPosition = Vector3.zero;
            labelObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
            TextMesh labelMesh = labelObject.AddComponent(typeof(TextMesh)) as TextMesh;

            if (symbology["body"].ContainsKey("Label") && properties.ContainsKey(symbology["body"].Label))
            {
                labelMesh.text = (string)properties[symbology["body"].Label];
            }

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
        Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
        List<Feature> features = new List<Feature>();
        foreach (Datapolygon dataFeature in dataFeatures)
        {
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
                throw new System.ArgumentException("This Polygon is not a Linear Ring", dataFeature.gisProperties.ToString());
            }
            List<LineString> LinearRings = new List<LineString>();
            LinearRings.Add(line);
            IDictionary<string, object> properties = dataFeature.gisProperties;
            DatapointSphere centroid = dataFeature.centroid;
            properties["polyhedral"] = new Point(Tools.Vect2Ipos(centroid.position));
            features.Add(new Feature(new Polygon(LinearRings), properties, dataFeature.gisId));
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
