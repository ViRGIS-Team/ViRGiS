// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;
using Project;
using UnityEngine.UI;

public class PointLayer : MonoBehaviour, ILayer
{
    // The prefab for the data points to be instantiated
    public GameObject SpherePrefab;
    public GameObject CubePrefab;
    public GameObject CylinderPrefab;
    public GameObject LabelPrefab;

    private GeoJsonReader geoJsonReader;

    public bool changed { get; set; }
    public RecordSet layer { get; set; }

    private void Start()
    {
        StartCoroutine(GetEvents());
    }

    public async Task<GameObject> Init(GeographyCollection layer)
    {
        // get geojson data
        this.layer = layer;
        AbstractMap _map = Global._map;
        Dictionary<string, Unit> symbology = layer.Properties.Units;
        GameObject PointPrefab = new GameObject();
        float displacement = 1.0f;
        if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape"))
        {
            Shapes shape = symbology["point"].Shape;
            switch (shape)
            {
                case Shapes.Spheroid:
                    PointPrefab = SpherePrefab;
                    break;
                case Shapes.Cuboid:
                    PointPrefab = CubePrefab;
                    break;
                case Shapes.Cylinder:
                    PointPrefab = CylinderPrefab;
                    displacement = 1.5f;
                    break;
            }
        } else
        {
            PointPrefab = SpherePrefab;
        }

        geoJsonReader = new GeoJsonReader();
        await geoJsonReader.Load(layer.Source);
        FeatureCollection myFC = geoJsonReader.getFeatureCollection();
        int id = 0;

        foreach (Feature feature in myFC.Features)
        {
            // Get the geometry
            MultiPoint mPoint = null;
            if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.Point)
            {
                mPoint = new MultiPoint(new List<Point>() { feature.Geometry as Point });
            }
            else if (feature.Geometry.Type == GeoJSON.Net.GeoJSONObjectType.MultiPoint)
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
                com.SetId(id);

                //Set the symbology
                if (symbology.ContainsKey("point"))
                {
                    dataPoint.SendMessage("SetColor", (Color)symbology["point"].Color);
                    dataPoint.transform.localScale = symbology["point"].Transform.Scale;
                    dataPoint.transform.localRotation = symbology["point"].Transform.Rotate;
                    dataPoint.transform.localPosition = symbology["point"].Transform.Position;
                    dataPoint.transform.position = position;
                }

                //Set the label
                GameObject labelObject = Instantiate(LabelPrefab, Vector3.zero, Quaternion.identity);
                labelObject.transform.parent = dataPoint.transform;
                labelObject.transform.localPosition = Vector3.up * displacement;
                Text labelText = labelObject.GetComponentInChildren<Text>();

                if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Label") && symbology["point"].Label != null && properties.ContainsKey(symbology["point"].Label))
                {
                    labelText.text = (string)properties[symbology["point"].Label];
                }

                id++;

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
            features.Add(new Feature(new Point(Tools.Vect2Ipos(pointFunc.gameObject.transform.position)), pointFunc.gisProperties, pointFunc.gisId));
        }
        FeatureCollection FC = new FeatureCollection(features);
        geoJsonReader.SetFeatureCollection(FC);
        await geoJsonReader.Save();

    }

    /// <summary>
    /// Called when a child component is translated by User action
    /// </summary>
    /// <param name="args">MoveArgs</param>
    public void Translate(MoveArgs args)
    {
        gameObject.BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
        changed = true;
    }

    /// <summary>
    /// Get the Eventmanager and set up the event listerners
    /// </summary>
    /// <returns></returns>
    IEnumerator GetEvents()
    {
        GameObject Map = Global.Map;
        EventManager eventManager;
        do
        {
            eventManager = Map.GetComponent<EventManager>();
            if (eventManager == null) { new WaitForSeconds(.5f); };
        } while (eventManager == null);
        AppState.instance.AddEndEditSessionListener(ExitEditsession);
        yield return eventManager;
    }

}
