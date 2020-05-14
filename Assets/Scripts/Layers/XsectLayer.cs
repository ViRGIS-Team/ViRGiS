// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
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
    public class XsectLayer: Layer<GeologyCollection, FeatureCollection>
    {

        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject PolygonPrefab; // Prefab to be used for the polygons
        public Material Mat; // Material to be used for the Polygon

        private GameObject HandlePrefab;
        private GameObject LinePrefab;

        private GeoJsonReader geoJsonReader;
        private List<Texture> Textures = new List<Texture>();


        protected override async Task _init(GeologyCollection layer)
        {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
            foreach (Feature feature in features.Features)
            {
               if (feature.Properties.ContainsKey("url") && feature.Properties["url"] != null) {
                    Texture tex = await TextureImage.Get(new Uri(feature.Properties["url"] as string));
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Textures.Add(tex);
                }
            }
        }

        protected override void _add(MoveArgs args)
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
                MultiLineString mLines = null;
                if (feature.Geometry.Type == GeoJSONObjectType.LineString) {
                    mLines = new MultiLineString(new List<LineString>() { feature.Geometry as LineString });
                } else if (feature.Geometry.Type == GeoJSONObjectType.MultiLineString) {
                    mLines = feature.Geometry as MultiLineString;
                }

                int index = 0;
                foreach(LineString line in mLines.Coordinates)
                {
                    Vector3 origin = Tools.Ipos2Vect(line.Point(0));


                    //Create the GameObjects
                    GameObject dataLine = Instantiate(LinePrefab, origin, Quaternion.identity);
                    GameObject dataPoly = Instantiate(PolygonPrefab, origin, Quaternion.identity);
                    dataPoly.transform.parent = gameObject.transform;
                    dataLine.transform.parent = dataPoly.transform;
m;

                    // add the gis data from geoJSON
                    Datapolygon com = dataPoly.GetComponent<Datapolygon>();
                    com.gisId = gisId;
                    com.gisProperties = properties;
                    com.Centroid = centroid.GetComponent<Datapoint>();
                    com.Centroid.SetColor((Color)symbology["point"].Color);

                    if (symbology["body"].ContainsKey("Label") && properties.ContainsKey(symbology["body"].Label))
                    {
                        //Set the label
                        GameObject labelObject = Instantiate(LabelPrefab, center, Quaternion.identity);
                        labelObject.transform.parent = centroid.transform;
                        labelObject.transform.Translate(Vector3.up * symbology["point"].Transform.Scale.magnitude);
                        Text labelText = labelObject.GetComponentInChildren<Text>();
                        labelText.text = (string)properties[symbology["body"].Label];
                    }

                    // Darw the LinearRing
                    Dataline Lr = dataLine.GetComponent<Dataline>();
                    Lr.Draw(perimeter, symbology, LinePrefab, HandlePrefab, null);


                    //Draw the Polygon
                    if (Textures.Count > 0) Mat.SetTexture("_BaseMap", Textures[index]);
                    com.Draw(Lr.VertexTable, Mat);
                    centroid.transform.localScale = symbology["point"].Transform.Scale;
                    centroid.transform.localRotation = symbology["point"].Transform.Rotate;
                    centroid.transform.localPosition = symbology["point"].Transform.Position;
                    index++;
                }
            };
        }

        protected override void ExitEditSession(bool saved) {
            BroadcastMessage("EditEnd", SendMessageOptions.DontRequireReceiver);
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
                    positions.Add(Tools.Vect2Ipos(vertex) as Position);
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
                properties["polyhedral"] = new Point(Tools.Vect2Ipos(centroid.transform.position));
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

        /*public override VirgisComponent GetClosest(Vector3 coords)
        {
            throw new System.NotImplementedException();
        }*/
    }
}
