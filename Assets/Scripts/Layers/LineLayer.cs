// copyright Runette Software Ltd, 2020. All rights reserved

using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;
using Project;

namespace ViRGIS
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLayer : Layer
    {
        // The prefab for the data points to be instantiated
        public GameObject LinePrefab;
        public GameObject HandlePrefab;
        public GameObject LabelPrefab;

        // used to read the GeoJSON file for this layer
        private GeoJsonReader geoJsonReader;

        public override async Task _init(GeographyCollection layer)
        {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
        }


        public override void _draw()
        {
            Dictionary<string, Unit> symbology = layer.Properties.Units;

            foreach (Feature feature in features.Features)
            {
                // Get the geometry
                MultiLineString mLines = null;
                if (feature.Geometry.Type == GeoJSONObjectType.LineString)
                {
                    mLines = new MultiLineString(new List<LineString>() { feature.Geometry as LineString });
                }
                else if (feature.Geometry.Type == GeoJSONObjectType.MultiLineString)
                {
                    mLines = feature.Geometry as MultiLineString;
                }

                IDictionary<string, object> properties = feature.Properties;
                string gisId = feature.Id;

                foreach (LineString line in mLines.Coordinates)
                {
                    GameObject dataLine = Instantiate(LinePrefab, Tools.Ipos2Vect(line.Point(0)), Quaternion.identity);
                    dataLine.transform.parent = gameObject.transform;

                    //set the gisProject properties
                    DatalineCylinder com = dataLine.GetComponent<DatalineCylinder>();
                    com.gisId = gisId;
                    com.gisProperties = properties;

                    //Draw the line
                    com.Draw(line, symbology, LinePrefab, HandlePrefab, LabelPrefab);
                }
            };
        }

        public override void ExitEditsession()
        {
            BroadcastMessage("EditEnd", SendMessageOptions.DontRequireReceiver);
        }

        public override GeographyCollection Save()
        {
            if (changed)
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
                geoJsonReader.Save();
            }
            return layer;
        }

        public override void Translate(MoveArgs args)
        {
            changed = true;
        }


        public override void MoveAxis(MoveArgs args)
        {
            changed = true;
        }
    }
}
