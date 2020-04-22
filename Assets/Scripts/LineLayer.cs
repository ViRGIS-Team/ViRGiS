// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;
using Project;


/// <summary>
/// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
/// </summary>
public class LineLayer : MonoBehaviour, ILayer
{
    // Name of the input file, no extension
    private string inputfile;

    // The prefab for the data points to be instantiated
    public GameObject LinePrefab;
    public GameObject HandlePrefab;
    public GameObject LabelPrefab;

    private GeoJsonReader geoJsonReader;

    public bool changed { get; set; } // shows if the data is dirty and should be saved
    public RecordSet layer { get; set; } // the layer RecordSet data

    private void Start()
    {
        StartCoroutine(GetEvents());
    }

    /// <summary>
    /// Loads the Layer data from the source file in the GeographyCollection data and draws the data
    /// </summary>
    /// <param name="layer"> A GeographyCollection</param>
    /// <returns></returns>
    public async Task<GameObject> Init(GeographyCollection layer)
    {
        this.layer = layer;
        // get geojson data
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
            com.Draw(lines[0], symbology, LinePrefab, HandlePrefab, LabelPrefab);
            //dataLine.GetComponentInChildren<TextMesh>().text = name + "," + type;

        };
        changed = false;
        return gameObject;
    }

    /// <summary>
    /// called when an Edit Session is ended
    /// </summary>
    public void ExitEditsession()
    {
        Save();
    }

    /// <summary>
    /// called to save the data. Only saves data that is dirty
    /// </summary>
    public async void Save()
    {
        DatalineCylinder[] dataFeatures = gameObject.GetComponentsInChildren<DatalineCylinder>();
        List<Feature> features = new List<Feature>();
        foreach (DatalineCylinder dataFeature in dataFeatures)
        {
            Vector3[] vertices = dataFeature.GetVerteces();
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

    /// <summary>
    /// Gets the EventManager, waiting for it to instantiate if it does not exist. Adss the listerners required :
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
        eventManager.EditSessionEndEvent.AddListener(ExitEditsession);
        yield return eventManager;
    }

}
