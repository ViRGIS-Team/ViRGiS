// copyright Runette Software Ltd, 2020. All rights reserved

using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;
using Project;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLayer : Layer<GeographyCollection, FeatureCollection>
    {
        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject LabelPrefab;

        // used to read the GeoJSON file for this layer
        private GeoJsonReader geoJsonReader;

        private GameObject HandlePrefab;
        private GameObject LinePrefab;


        public override async Task _init(GeographyCollection layer)
        {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
        }

        public override void _add(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        public override void _draw()
        {
            Dictionary<string, Unit> symbology = layer.Properties.Units;
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
                    default:
                        HandlePrefab = SpherePrefab;
                        break;
                }
            }
            else
            {
                HandlePrefab = SpherePrefab;
            }

            if (symbology.ContainsKey("line") && symbology["line"].ContainsKey("Shape"))
            {
                Shapes shape = symbology["line"].Shape;
                switch (shape)
                {
                    case Shapes.Cuboid:
                        LinePrefab = CuboidLinePrefab;
                        break;
                    case Shapes.Cylinder:
                        LinePrefab = CylinderLinePrefab;
                        break;
                    default:
                        LinePrefab = CylinderLinePrefab;
                        break;
                }
            }
            else
            {
                LinePrefab = CylinderLinePrefab;
            }


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
                    Dataline com = dataLine.GetComponent<Dataline>();
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

        public override void _cp() { }

        public override void _save()
        {
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            List<Feature> features = new List<Feature>();
            foreach (Dataline dataFeature in dataFeatures)
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

        public override void Translate(MoveArgs args)
        {
            changed = true;
        }


        public override void MoveAxis(MoveArgs args)
        {
            changed = true;
        }

       /* public override VirgisComponent GetClosest(Vector3 coords)
        {
            throw new System.NotImplementedException();
        }*/

    }
}
