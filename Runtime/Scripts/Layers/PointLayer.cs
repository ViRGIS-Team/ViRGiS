// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using OSGeo.OGR;
using System.Linq;


namespace Virgis {

    public class PointLayer : VirgisLayer<RecordSet, Layer> {
        // The prefab for the data points to be instantiated
        public GameObject SpherePrefab;
        public GameObject CubePrefab;
        public GameObject CylinderPrefab;
        public GameObject LabelPrefab;
        public Material BaseMaterial;

        private GameObject PointPrefab;
        private Dictionary<string, Unit> symbology;
        private float displacement;
        private Material mainMat;
        private Material selectedMat;

        private void Start() {
            featureType = FeatureType.POINT;
        }

        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
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
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            VirgisFeature newFeature = _drawFeature(geometry[0], new Feature(new FeatureDefn(null)));
            changed = true;
            return newFeature;
        }



        protected override async Task _draw() {
            RecordSet layer = GetMetadata();
            if (layer.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1], layer.Properties.BBox[2], layer.Properties.BBox[3]);
            }
            SetCrs(OgrReader.getSR(features, layer));
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.features) {
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry point = feature.GetGeomFieldRef(j);
                        wkbGeometryType type = point.GetGeometryType();
                        string t = type.ToString();
                        if (point.GetGeometryType() == wkbGeometryType.wkbPoint ||
                            point.GetGeometryType() == wkbGeometryType.wkbPoint25D ||
                            point.GetGeometryType() == wkbGeometryType.wkbPointM ||
                            point.GetGeometryType() == wkbGeometryType.wkbPointZM) {
                            point.TransformWorld(GetCrs()).ToList<Vector3>().ForEach(async item => await _drawFeatureAsync(item, feature));
                        } else if
                           (point.GetGeometryType() == wkbGeometryType.wkbMultiPoint ||
                            point.GetGeometryType() == wkbGeometryType.wkbMultiPoint25D ||
                            point.GetGeometryType() == wkbGeometryType.wkbMultiPointM ||
                            point.GetGeometryType() == wkbGeometryType.wkbMultiPointZM) {
                            int n = point.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                Geometry Point2 = point.GetGeometryRef(k);
                                Point2.TransformWorld(GetCrs()).ToList<Vector3>().ForEach(async item => await _drawFeatureAsync(item, feature));
                            }
                        }
                        point.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }

        /// <summary>
        /// Draws a single feature based on world space coordinates
        /// </summary>
        /// <param name="position"> Vector3 position</param>
        /// <param name="feature">Feature (optional)</param>

        protected VirgisFeature _drawFeature(Vector3 position, Feature feature = null) {
            //instantiate the prefab with coordinates defined above
            GameObject dataPoint = Instantiate(PointPrefab, transform, false);
            dataPoint.transform.position = position;

            // add the gis data from source
            Datapoint com = dataPoint.GetComponent<Datapoint>();
            if (feature != null) com.feature = feature;
            com.SetMaterial(mainMat, selectedMat);

            

            //Set the symbology
            if (symbology.ContainsKey("point")) {
                dataPoint.transform.localScale = symbology["point"].Transform.Scale;
                dataPoint.transform.localRotation = symbology["point"].Transform.Rotate;
                dataPoint.transform.Translate(symbology["point"].Transform.Position, Space.Self);
            }


            //Set the label
            if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Label") && symbology["point"].Label != null && (feature?.ContainsKey(symbology["point"].Label) ?? false)) {
                GameObject labelObject = Instantiate(LabelPrefab, dataPoint.transform, false);
                labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                labelObject.transform.localPosition = Vector3.up * displacement;
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string) feature.Get(symbology["point"].Label);
            }

            return com;
        }

        protected Task<int> _drawFeatureAsync(Vector3 position, Feature feature = null) {
            Task<int> t1 = new Task<int>(() => {
                _drawFeature(position, feature);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override void _checkpoint() {
        }
        protected override Task _save() {
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            List<Feature> thisFeatures = new List<Feature>();
            long n = features.GetFeatureCount(0);
            for (int i = 0; i < (int) n; i++) features.DeleteFeature(i);
            foreach (Datapoint pointFunc in pointFuncs) {
                Feature feature = pointFunc.feature;
                Geometry geom = (pointFunc.gameObject.transform.position.ToGeometry());
                geom.TransformTo(GetCrs());
                feature.SetGeometryDirectly(geom);
                features.CreateFeature(feature);
            }
            features.SyncToDisk();
            return Task.CompletedTask;
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
                Destroy(vertex.gameObject);
            }
        }
    }
}
