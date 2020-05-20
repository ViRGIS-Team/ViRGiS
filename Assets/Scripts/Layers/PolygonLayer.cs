// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using GeoJSON.Net;
using System.Threading.Tasks;
using Project;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLayer : Layer<GeographyCollection, FeatureCollection>
    {

        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject PolygonPrefab; // Prefab to be used for the polygons
        public GameObject LabelPrefab; // Prefab to used for the Labels
        public Material Mat; // Material to be used for the Polygon

        private GameObject HandlePrefab;
        private GameObject LinePrefab;

        private GeoJsonReader geoJsonReader;


        protected override async Task _init(GeographyCollection layer)
        {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
        }

        protected override void _addFeature(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        protected override void _draw()
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
                IDictionary<string, object> properties = feature.Properties;
                string gisId = feature.Id;


                // Get the geometry
                MultiPolygon mPols = null;
                if (feature.Geometry.Type == GeoJSONObjectType.Polygon)
                {
                    mPols = new MultiPolygon(new List<Polygon>() { feature.Geometry as Polygon });
                }
                else if (feature.Geometry.Type == GeoJSONObjectType.MultiPolygon)
                {
                    mPols = feature.Geometry as MultiPolygon;
                }

                foreach (Polygon mPol in mPols.Coordinates)
                {
                    ReadOnlyCollection<LineString> LinearRings = mPol.Coordinates;
                    LineString perimeter = LinearRings[0];
                    Vector3[] poly = perimeter.Vector3();
                    Vector3 center = Vector3.zero;
                    if (properties.ContainsKey("polyhedral") && properties["polyhedral"] != null)
                    {
                        if (properties["polyhedral"].GetType() != typeof(Point))
                        {
                            JObject jobject = (JObject)properties["polyhedral"];
                            Point centerPoint = jobject.ToObject<Point>();
                            center = centerPoint.Coordinates.Vector3();
                            properties["polyhedral"] = center.ToPoint();
                        } else
                        {
                            center = (properties["polyhedral"] as Point).Coordinates.Vector3();
                        }
                    }
                    else
                    {
                        center = Datapolygon.FindCenter(poly);
                        properties["polyhedral"] = center.ToPoint();
                    }

                    //Create the GameObjects
                    GameObject dataPoly = Instantiate(PolygonPrefab, center, Quaternion.identity, transform);
                    GameObject dataLine = Instantiate(LinePrefab,  dataPoly.transform, false);
                    GameObject centroid = Instantiate(HandlePrefab,  dataLine.transform, false);

                    // add the gis data from geoJSON
                    Datapolygon com = dataPoly.GetComponent<Datapolygon>();
                    com.gisId = gisId;
                    com.gisProperties = properties;
                    com.Centroid = centroid.GetComponent<Datapoint>();
                    //com.Centroid.SetColor((Color)symbology["point"].Color);

                    if (symbology["body"].ContainsKey("Label") && properties.ContainsKey(symbology["body"].Label))
                    {
                        //Set the label
                        GameObject labelObject = Instantiate(LabelPrefab, centroid.transform, false );
                        labelObject.transform.Translate(centroid.transform.TransformVector(Vector3.up) * symbology["point"].Transform.Scale.magnitude, Space.Self);
                        Text labelText = labelObject.GetComponentInChildren<Text>();
                        labelText.text = (string)properties[symbology["body"].Label];
                    }

                    // Darw the LinearRing
                    Dataline Lr = dataLine.GetComponent<Dataline>();
                    //Lr.Draw(perimeter, symbology, LinePrefab, HandlePrefab, null);


                    //Draw the Polygon
                    Mat.SetColor("_BaseColor", symbology["body"].Color);
                    com.Draw(Lr.VertexTable, Mat);
                    

                    centroid.transform.localScale = symbology["point"].Transform.Scale;
                    centroid.transform.localRotation = symbology["point"].Transform.Rotate;
                    centroid.transform.localPosition = symbology["point"].Transform.Position;
                }
            };
        }

        protected override void _checkpoint() { }
        protected override void _save()
        {
            Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            List<Feature> thisFeatures = new List<Feature>();
            foreach (Datapolygon dataFeature in dataFeatures)
            {
                Dataline perimeter = dataFeature.GetComponentInChildren<Dataline>();
                Vector3[] vertices = perimeter.GetVerteces();
                List<Position> positions = new List<Position>();
                foreach (Vector3 vertex in vertices)
                {
                    positions.Add(vertex.ToPosition() as Position);
                }
                LineString line = new LineString(positions);
                if (!line.IsLinearRing())
                {
                    Debug.LogError("This Polygon is not a Linear Ring");
                    return;
                }
                List<LineString> LinearRings = new List<LineString>();
                LinearRings.Add(line);
                IDictionary<string, object> properties = dataFeature.gisProperties;
                Datapoint centroid = dataFeature.Centroid;
                properties["polyhedral"] = centroid.transform.position.ToPoint();
                thisFeatures.Add(new Feature(new Polygon(LinearRings), properties, dataFeature.gisId));
            };
            FeatureCollection FC = new FeatureCollection(thisFeatures);
            geoJsonReader.SetFeatureCollection(FC);
            geoJsonReader.Save();
            features = FC;
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
