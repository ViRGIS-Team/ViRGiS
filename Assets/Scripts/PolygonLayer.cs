// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Mapbox.Unity.Map;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;
using Project;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;

/// <summary>
/// Controls an instance of a Polygon Layer
/// </summary>
public class PolygonLayer : MonoBehaviour, ILayer
{

    // Name of the input file, no extension
    private string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;   // Prefab to be used to build the perimeter line
    public GameObject SpherePrefab; // prefab to be used for Vertex handles
    public GameObject CubePrefab; // prefab to be used for Vertex handles
    public GameObject CylinderPrefab; // prefab to be used for Vertex handle
    public GameObject PolygonPrefab; // Prefab to be used for the polygons
    public GameObject LabelPrefab; // Prefab to used for the Labels
    public Material Mat; // Material to be used for the Polygon

    private GeoJsonReader geoJsonReader;
    public RecordSet layer { get; set; } // The layer RecordSet data
    public bool changed { get; set; }  // whether the data is dirty and should be saved
     
    private void Start()
    {
        StartCoroutine(GetEvents());
    }

    /// <summary>
    /// Fetch the data from the source fule in the GeogpraphyCollection and create the Features
    /// </summary>
    /// <param name="layer"> GeographyCollection</param>
    /// <returns></returns>
    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;

        // get geojson data
        AbstractMap _map = Global._map;
        inputfile = layer.Source;
        Dictionary<string, Unit> symbology = layer.Properties.Units;
        GameObject HandlePrefab = new GameObject();
        if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape"))
        {
            Shapes shape = symbology["point"].Shape;
            switch (shape)
            {
                case Shapes.Spheroid:
                    HandlePrefab = SpherePrefab;
                    break;
                case Shapes.Cuboid:
                    HandlePrefab = CubePrefab;
                    break;
                case Shapes.Cylinder:
                    HandlePrefab = CylinderPrefab;
                    break;
            }
        }
        else
        {
            HandlePrefab = SpherePrefab;
        }


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
            Vector3[] poly = Tools.LS2Vect(perimeter);
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




            //Set the label
            GameObject labelObject = Instantiate(LabelPrefab, center, Quaternion.identity);
            labelObject.transform.parent = centroid.transform;
            labelObject.transform.Translate(Vector3.up * symbology["point"].Transform.Scale.magnitude);
            Text labelText = labelObject.GetComponentInChildren<Text>();

            if (symbology["body"].ContainsKey("Label") && properties.ContainsKey(symbology["body"].Label))
            {
                labelText.text = (string)properties[symbology["body"].Label];
            }

            //Draw the Polygon
            Mat.SetColor("_BaseColor", symbology["body"].Color);
            com.Draw(perimeter, Mat);
            dataLine.GetComponent<DatalineCylinder>().Draw(perimeter, symbology, LinePrefab, HandlePrefab, null);
            centroid.SendMessage("SetColor", (Color)symbology["point"].Color);
            centroid.SendMessage("SetId", -1);
            centroid.transform.localScale = symbology["point"].Transform.Scale;
            centroid.transform.localRotation = symbology["point"].Transform.Rotate;
            centroid.transform.localPosition = symbology["point"].Transform.Position;

        };
        changed = false;
        return gameObject;

    }

    /// <summary>
    /// Called when an Edit Session ends
    /// </summary>
    public void ExitEditsession()
    {
        Save();
    }

    /// <summary>
    /// Called when the layer is saved. Only Save Dirty data
    /// </summary>
    public async void Save()
    {
        Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
        List<Feature> features = new List<Feature>();
        foreach (Datapolygon dataFeature in dataFeatures)
        {
            DatalineCylinder perimeter = dataFeature.GetComponentInChildren<DatalineCylinder>();
            Vector3[] vertices = perimeter.GetVerteces();
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
            properties["polyhedral"] = new Point(Tools.Vect2Ipos(centroid.transform.position));
            features.Add(new Feature(new Polygon(LinearRings), properties, dataFeature.gisId));
        };
        FeatureCollection FC = new FeatureCollection(features);
        geoJsonReader.SetFeatureCollection(FC);
        await geoJsonReader.Save();
    }

    /// <summary>
    /// Gets the EventManager, waiting for it to be instantiated if neccesary, and adds the appropriate events :
    /// ExitEditSession,
    /// </summary>
    /// <returns>EventManager</returns>
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
