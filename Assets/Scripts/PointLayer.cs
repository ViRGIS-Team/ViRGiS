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
using Project;

public class PointLayer : MonoBehaviour, Layer
{
    // The prefab for the data points to be instantiated
    public GameObject PointPrefab;

    private GeoJsonReader geoJsonReader;

    public bool changed { get; set; }
    public RecordSet layer { get; set; }

    private void Start()
    {
        StartCoroutine(GetEvents());
    }

    public async Task<GameObject> Init (GeographyCollection layer)
    {
        // get geojson data
        this.layer = layer;
        AbstractMap _map = Global._map;
        Dictionary<string, Unit> symbology = layer.Properties.Units;

        geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(layer.Source);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();

        foreach (Feature feature in myFC.Features)
        {
            // Get the geometry
            MultiPoint mPoint = null;
            if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.Point) {
                mPoint = new MultiPoint(new List<Point>() { feature.Geometry as Point });
            } else if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.MultiPoint)
            {
                mPoint = feature.Geometry as MultiPoint;
            }

            Dictionary<string, object> properties = feature.Properties as Dictionary<string, object>;
            string gisId = feature.Id;
            foreach (Point geometry in mPoint.Coordinates)
            {
                Position in_position = geometry.Coordinates as Position;
                Vector3 position = Tools.Ipos2Vect(in_position);
                //Vector2d _location = new Vector2d(in_position.Latitude, in_position.Longitude);

                //float y = (float)in_position.Altitude;
                //float y = _map.QueryElevationInMetersAt(_location);

                //instantiate the prefab with coordinates defined above
                GameObject dataPoint = Instantiate(PointPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                dataPoint.transform.parent = gameObject.transform;

                // add the gis data from geoJSON
                DatapointSphere com = dataPoint.GetComponent<DatapointSphere>();
                com.gisId = gisId;
                com.gisProperties = properties;

                //Set the label
                GameObject labelObject = new GameObject();
                labelObject.transform.parent = dataPoint.transform;
                labelObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
                TextMesh labelMesh = labelObject.AddComponent(typeof(TextMesh)) as TextMesh;

                if (symbology.ContainsKey("default") && symbology["default"].ContainsKey("Label") && symbology["default"].Label != null && properties.ContainsKey(symbology["default"].Label))
                {
                    labelMesh.text = (string)properties[symbology["default"].Label];
                }


                //Set the symbology
                dataPoint.SendMessage("SetColor", (Color)symbology["default"].Color);
                dataPoint.transform.localScale = symbology["default"].Transform.Scale;
                dataPoint.transform.localRotation = symbology["default"].Transform.Rotate;
                dataPoint.transform.localPosition = symbology["default"].Transform.Position;
                dataPoint.transform.position = position;
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
        DatapointSphere[] pointFuncs = gameObject.GetComponentsInChildren<DatapointSphere>();
        List<Feature> features = new List<Feature>();
        foreach (DatapointSphere pointFunc in pointFuncs)
        {
            features.Add( new Feature(new Point(Tools.Vect2Ipos(pointFunc.gameObject.transform.position)),pointFunc.gisProperties, pointFunc.gisId));
        }
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
