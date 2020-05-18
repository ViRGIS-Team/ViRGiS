// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Project;
using System;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class XsectLayer: Layer<GeologyCollection, EgbFeatureCollection>
    {

        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject PolygonPrefab; // Prefab to be used for the polygons
        public Material Mat; // Material to be used for the Polygon
        public string ImageSource;

        private GameObject HandlePrefab;
        private GameObject LinePrefab;

        private EgbReader egbReader;
        Texture2D tex;


        protected override async Task _init(GeologyCollection layer)
        {
            egbReader = new EgbReader();
            await egbReader.Load(layer.Source);
            egbReader.Read();
            features = egbReader.features;
        }

        protected override void _addFeature(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        protected async override void _draw()
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


            foreach (EgbFeature feature in features)
            {
                Dictionary<string, object> properties = feature.image;
                string gisId = feature.Id;


                // Get the geometry
          
                Vector3 origin = feature.bottom.Point(0).Vector3();
                Vector3[] top = feature.top.Vector3();
                Vector3[] bottom = feature.bottom.Vector3();
                int length = top.Length;

                Vector3[]  poly = new Vector3[4] { bottom[1], top[1], top[0],bottom[0]};
                Vector3 center = Datapolygon.FindCenter(poly);

                List<VertexLookup> VertexTable = new List<VertexLookup>();
                VertexTable.Add(new VertexLookup() { Id = System.Guid.NewGuid(), Vertex = -1 });
                for (int i = 0; i < 4; i++) {
                    GameObject point = Instantiate(HandlePrefab, poly[i], Quaternion.identity, transform);
                    VertexTable.Add(new VertexLookup() { Id = System.Guid.NewGuid(), Vertex = i, Com = point.GetComponent<Datapoint>() });
                }

                tex = null;
                if (feature.image.ContainsKey("Image") && feature.image["Image"] != null) {
                    string Url = ImageSource + feature.image["Image"] as string;
                    tex = await TextureImage.Get(new Uri(Url));
                    if (tex != null) {
                        tex.wrapMode = TextureWrapMode.Clamp;
                    }
                }


                //Create the GameObjects
                // GameObject dataLine = Instantiate(LinePrefab, origin, Quaternion.identity);
                GameObject dataPoly = Instantiate(PolygonPrefab, origin, Quaternion.identity, transform);
                   // dataPoly.transform.parent = gameObject.transform;
                   // dataLine.transform.parent = dataPoly.transform;

                   // add the gis data from geoJSON
                Datapolygon com = dataPoly.GetComponent<Datapolygon>();
                com.gisId = gisId;
                com.gisProperties = properties;
                com.Centroid = center;
                   //// com.Centroid = centroid.GetComponent<Datapoint>();
                   // com.Centroid.SetColor((Color)symbology["point"].Color);

                   // if (symbology["body"].ContainsKey("Label") && properties.ContainsKey(symbology["body"].Label))
                   // {
                   //     //Set the label
                   //    // GameObject labelObject = Instantiate(LabelPrefab, center, Quaternion.identity);
                   //    // labelObject.transform.parent = centroid.transform;
                   //     //labelObject.transform.Translate(Vector3.up * symbology["point"].Transform.Scale.magnitude);
                   //     //Text labelText = labelObject.GetComponentInChildren<Text>();
                   //    //labelText.text = (string)properties[symbology["body"].Label];
                   // }

                   // // Darw the LinearRing
                   // Dataline Lr = dataLine.GetComponent<Dataline>();
                   // //Lr.Draw(perimeter, symbology, LinePrefab, HandlePrefab, null);


                   // //Draw the Polygon

                com.Draw(VertexTable, Mat);
                Material newMat = dataPoly.GetComponentInChildren<Renderer>().material;
                if (tex != null) newMat.SetTexture("_BaseMap", tex);
                // //centroid.transform.localScale = symbology["point"].Transform.Scale;
                //// centroid.transform.localRotation = symbology["point"].Transform.Rotate;
                //// centroid.transform.localPosition = symbology["point"].Transform.Position;
                // index++;
                //}
            }
        }

        protected override void _checkpoint() { }
        protected override void _save()
        {
            //Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            //List<Feature> thisFeatures = new List<Feature>();
            //foreach (Datapolygon dataFeature in dataFeatures)
            //{
            //    Dataline perimeter = dataFeature.GetComponentInChildren<Dataline>();
            //    Vector3[] vertices = perimeter.GetVerteces();
            //    List<Position> positions = new List<Position>();
            //    foreach (Vector3 vertex in vertices)
            //    {
            //        positions.Add(vertex.ToPosition() as Position);
            //    }
            //    LineString line = new LineString(positions);
            //    if (!line.IsLinearRing())
            //    {
            //        Debug.LogError("This Polygon is not a Linear Ring");
            //        return;
            //    }
            //    List<LineString> LinearRings = new List<LineString>();
            //    LinearRings.Add(line);
            //    IDictionary<string, object> properties = dataFeature.gisProperties;
            //    Datapoint centroid = dataFeature.Centroid;
            //    properties["polyhedral"] = centroid.transform.position.ToPoint();
            //    thisFeatures.Add(new Feature(new Polygon(LinearRings), properties, dataFeature.gisId));
            //};
            //FeatureCollection FC = new FeatureCollection(thisFeatures);
            //geoJsonReader.SetFeatureCollection(FC);
            //geoJsonReader.Save();
            //features = FC;
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
