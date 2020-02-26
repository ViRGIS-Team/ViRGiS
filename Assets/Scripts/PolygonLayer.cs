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

public class PolygonLayer : MonoBehaviour
{

    // Name of the input file, no extension
    public string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;
    public GameObject HandlePrefab;
    public GameObject PolygonPrefab;
    public Material Mat;


    public void Init(AbstractMap _map)
    {
        // get geojson data

        GeoJsonReader geoJsonReader = new GeoJsonReader();
        geoJsonReader.Load(inputfile);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();

        foreach (Feature feature in myFC.Features)
        {
            Polygon geometry = feature.Geometry as Polygon;
            IDictionary<string, object> properties = feature.Properties;
            string name = (string)properties["name"];
            string type = (string)properties["type"];

            ReadOnlyCollection<LineString> LinearRings = geometry.Coordinates;
            LineString perimeter = LinearRings[0];
            GameObject dataLine = Instantiate(LinePrefab, Tools.Ipos2Vect(perimeter.Coordinates[0], 0, _map), Quaternion.identity);
            dataLine.GetComponent<DatalineCylinder>().Draw(perimeter, Color.red, 0.5f, LinePrefab, HandlePrefab, _map);
            //dataLine.GetComponentInChildren<TextMesh>().text = name + "," + type;
            Vector3[] poly = Tools.LS2Vect(perimeter, 0.0f, _map);
            Vector3 center = Poly.FindCenter(poly);
            GameObject dataPoly = Instantiate(PolygonPrefab, center, Quaternion.identity);
            dataPoly.transform.parent = gameObject.transform;
            dataLine.transform.parent = dataPoly.transform;
            Poly.Draw(poly, center, dataPoly, Mat);
        };

    }

}
