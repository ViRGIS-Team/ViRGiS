// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using GeoJSON.Net;
using System.Threading.Tasks;
using Project;
using UnityEngine.UI;
using Zinnia.Extension;
using System.Reflection.Emit;

namespace Virgis
{

    public class PointLayer : Layer<GeographyCollection, FeatureCollection>
    {
        // The prefab for the data points to be instantiated
        public GameObject SpherePrefab;
        public GameObject CubePrefab;
        public GameObject CylinderPrefab;
        public GameObject LabelPrefab;
        public Material BaseMaterial;

        // used to read the GeoJSON file for this layer
        private GeoJsonReader geoJsonReader;

        private GameObject PointPrefab;
        private Dictionary<string, Unit> symbology;
        private float displacement;
        private Material mainMat;
        private Material selectedMat;

        protected override async Task _init(GeographyCollection layer)
        {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
            symbology = layer.Properties.Units;
            displacement = 1.0f;
            if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape")) {
                Shapes shape = symbology["point"].Shape;
                switch (shape) {
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
                    default:
                        PointPrefab = SpherePrefab;
                        break;
                }
            } else {
                PointPrefab = SpherePrefab;
            }

            mainMat = Instantiate(BaseMaterial);
            mainMat.SetColor("_BaseColor", symbology.ContainsKey("point") ? (Color) symbology["point"].Color : Color.white);
            selectedMat = Instantiate(BaseMaterial);
            selectedMat.SetColor("_baseColor", symbology.ContainsKey("point") ? Color.white - (Color) symbology["point"].Color : Color.red);
        }

        protected override void _addFeature(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        protected override void _draw()
        {
            foreach (Feature feature in features.Features)
            {
                // Get the geometry
                MultiPoint mPoint = null;
                if (feature.Geometry.Type == GeoJSONObjectType.Point)
                {
                    mPoint = new MultiPoint(new List<Point>() { feature.Geometry as Point });
                }
                else if (feature.Geometry.Type == GeoJSONObjectType.MultiPoint)
                {
                    mPoint = feature.Geometry as MultiPoint;
                }

                Dictionary<string, object> properties = feature.Properties as Dictionary<string, object>;
                string gisId = feature.Id;
                foreach (Point geometry in mPoint.Coordinates)
                {
                    Position in_position = geometry.Coordinates as Position;
                    Vector3 position = in_position.Vector3();

                    //instantiate the prefab with coordinates defined above
                    GameObject dataPoint = Instantiate(PointPrefab, transform, false);
                    dataPoint.transform.position = position;

                    // add the gis data from geoJSON
                    Datapoint com = dataPoint.GetComponent<Datapoint>();
                    com.gisId = gisId;
                    com.gisProperties = properties;
                    com.SetMaterial(mainMat, selectedMat);

                    //Set the symbology
                    if (symbology.ContainsKey("point"))
                    {
                        dataPoint.transform.localScale = symbology["point"].Transform.Scale;
                        dataPoint.transform.localRotation = symbology["point"].Transform.Rotate;
                        dataPoint.transform.Translate(symbology["point"].Transform.Position, Space.Self);
                    }


                    //Set the label
                    GameObject labelObject = Instantiate(LabelPrefab,  dataPoint.transform, false);
                    labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                    labelObject.transform.localPosition = Vector3.up * displacement;
                    Text labelText = labelObject.GetComponentInChildren<Text>();

                    if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Label") && symbology["point"].Label != null && properties.ContainsKey(symbology["point"].Label))
                    {
                        labelText.text = (string)properties[symbology["point"].Label];
                    }
                }
            };
        }

        protected override void _checkpoint() { }
        protected override void _save()
        {
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            List<Feature> thisFeatures = new List<Feature>();
            foreach (Datapoint pointFunc in pointFuncs)
            {
                thisFeatures.Add(new Feature(pointFunc.gameObject.transform.position.ToPoint(), pointFunc.gisProperties, pointFunc.gisId));
            }
            FeatureCollection FC = new FeatureCollection(thisFeatures);
            geoJsonReader.SetFeatureCollection(FC);
            geoJsonReader.Save();
            features = FC;
        }


        public override void Translate(MoveArgs args)
        {
            gameObject.BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
            changed = true;
        }

        public override void MoveAxis(MoveArgs args)
        {

        }
    }
}
