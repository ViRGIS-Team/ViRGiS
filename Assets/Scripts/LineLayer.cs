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

public class LineLayer : MonoBehaviour
{
    // Name of the input file, no extension
    public string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;
    public GameObject HandlePrefab;


    // Start is called before the first frame update
    public void Init(AbstractMap _map)
    {
        // get geojson data

        GeoJsonReader geoJsonReader = new GeoJsonReader();
        geoJsonReader.Load(inputfile);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();


        foreach (Feature feature in myFC.Features)
        {
            // Get the geometry
            MultiLineString geometry = feature.Geometry as MultiLineString;
            IDictionary<string, object> properties = feature.Properties;
            string name = (string)properties["name"];
            string type = (string)properties["type"];
            ReadOnlyCollection<LineString> lines = geometry.Coordinates;
            GameObject dataLine = Instantiate(LinePrefab, Tools.Ipos2Vect(lines[0].Coordinates[0], _map), Quaternion.identity);
            dataLine.transform.parent = gameObject.transform;
            dataLine.GetComponent<DatalineCylinder>().Draw(lines[0], Color.red, 0.5f, LinePrefab, HandlePrefab, _map);
            dataLine.GetComponentInChildren<TextMesh>().text = name + "," + type;

        };
    }

}
