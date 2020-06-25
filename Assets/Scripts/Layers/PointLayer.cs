// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using OSGeo.OGR;
using System.Linq;

namespace Virgis {

    public class PointLayer : VirgisLayer<GeographyCollection, Layer> {
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

        protected override async Task _init(GeographyCollection layer) {
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

            Color col = symbology.ContainsKey("point") ? (Color) symbology["point"].Color : Color.white;
            Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            mainMat = Instantiate(BaseMaterial);
            mainMat.SetColor("_BaseColor", col);
            selectedMat = Instantiate(BaseMaterial);
            selectedMat.SetColor("_BaseColor", sel);
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            VirgisFeature newFeature = _drawFeature(geometry[0]);
            changed = true;
            return newFeature;
        }



        protected override void _draw() {
            long FeatureCount = features.GetFeatureCount(1);
            for (int i = 0; i < FeatureCount; i++) {
                Feature feature = features.GetFeature(i);
                string properties = feature.GetNativeData();
                //string gisId = feature.Id;
                Geometry point = feature.GetGeometryRef();
                if (point.GetGeometryType() == wkbGeometryType.wkbPoint || point.GetGeometryType() == wkbGeometryType.wkbPoint25D || point.GetGeometryType() == wkbGeometryType.wkbPointM || point.GetGeometryType() == wkbGeometryType.wkbPointZM) {
                    point.TransformWorld().ToList<Vector3>().ForEach(item => _drawFeature(item));
                }
            }
        }

        /// <summary>
        /// Draws a single feature based on world space coordinates
        /// </summary>
        /// <param name="position"> Vector3 position</param>
        /// <param name="gisId">string Id</param>
        /// <param name="properties">Dictionary properties</param>
        protected VirgisFeature _drawFeature(Vector3 position, string gisId = null, Dictionary<string, object> properties = null) {
            //instantiate the prefab with coordinates defined above
            GameObject dataPoint = Instantiate(PointPrefab, transform, false);
            dataPoint.transform.position = position;

            // add the gis data from geoJSON
            Datapoint com = dataPoint.GetComponent<Datapoint>();
            com.gisId = gisId;
            com.gisProperties = properties ?? new Dictionary<string, object>();
            com.SetMaterial(mainMat, selectedMat);

            //Set the symbology
            if (symbology.ContainsKey("point")) {
                dataPoint.transform.localScale = symbology["point"].Transform.Scale;
                dataPoint.transform.localRotation = symbology["point"].Transform.Rotate;
                dataPoint.transform.Translate(symbology["point"].Transform.Position, Space.Self);
            }


            //Set the label
            if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Label") && symbology["point"].Label != null && (properties?.ContainsKey(symbology["point"].Label) ?? false)) {
                GameObject labelObject = Instantiate(LabelPrefab, dataPoint.transform, false);
                labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                labelObject.transform.localPosition = Vector3.up * displacement;
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string) properties[symbology["point"].Label];
            }

            return com;
        }

        protected override void _checkpoint() {
        }
        protected override void _save() {
            //Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            //List<Feature> thisFeatures = new List<Feature>();
            //foreach (Datapoint pointFunc in pointFuncs) {
            //    thisFeatures.Add(new Feature(pointFunc.gameObject.transform.position.ToPoint(), pointFunc.gisProperties, pointFunc.gisId));
            //}
            //FeatureCollection FC = new FeatureCollection(thisFeatures);
            //geoJsonReader.SetFeatureCollection(FC);
            //geoJsonReader.Save();
            //features = FC;
        }

        public override GameObject GetFeatureShape() {
            GameObject fs = Instantiate(PointPrefab);
            Datapoint com = fs.GetComponent<Datapoint>();
            com.SetMaterial(mainMat, selectedMat);
            return fs;
        }

        public override void Translate(MoveArgs args) {
            gameObject.BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
            changed = true;
        }

        public override void MoveAxis(MoveArgs args) {

        }

        public void RemoveVertex(VirgisFeature vertex) {
            if (AppState.instance.InEditSession() && IsEditable()) {
                vertex.gameObject.Destroy();
            }
        }
    }
}
