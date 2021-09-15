// copyright Runette Software Ltd, 2020. All rights reserved

using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using OSGeo.OGR;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLayer : VirgisLayer<RecordSet, Layer>
    {

        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject PolygonPrefab; // Prefab to be used for the polygons
        public GameObject LabelPrefab; // Prefab to used for the Labels
        public Material PointBaseMaterial;
        public Material LineBaseMaterial;
        public Material BodyBaseMaterial;

        private GameObject HandlePrefab;
        private GameObject LinePrefab;

        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;
        private Material lineMain;
        private Material lineSelected;
        private Material bodyMain;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.POLYGON;
        }

        private void OnDestroy() {
            return;
        }


        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
                symbology = layer.Properties.Units;

                if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape")) {
                    Shapes shape = symbology["point"].Shape;
                    switch (shape) {
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
                } else {
                    HandlePrefab = SpherePrefab;
                }

                if (symbology.ContainsKey("line") && symbology["line"].ContainsKey("Shape")) {
                    Shapes shape = symbology["line"].Shape;
                    switch (shape) {
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
                } else {
                    LinePrefab = CylinderLinePrefab;
                }

                Color col = symbology.ContainsKey("point") ? (Color) symbology["point"].Color : Color.white;
                Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
                Color line = symbology.ContainsKey("line") ? (Color) symbology["line"].Color : Color.white;
                Color lineSel = symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
                Color body = symbology.ContainsKey("body") ? (Color) symbology["body"].Color : Color.white;
                mainMat = Instantiate(PointBaseMaterial);
                mainMat.SetColor("_BaseColor", col);
                selectedMat = Instantiate(PointBaseMaterial);
                selectedMat.SetColor("_BaseColor", sel);
                lineMain = Instantiate(LineBaseMaterial);
                lineMain.SetColor("_BaseColor", line);
                lineSelected = Instantiate(LineBaseMaterial);
                lineSelected.SetColor("_BaseColor", lineSel);
                bodyMain = Instantiate(BodyBaseMaterial);
                bodyMain.SetColor("_BaseColor", body);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override VirgisFeature _addFeature(Vector3[] line)
        {
            changed = true;
            Geometry geom = new Geometry(wkbGeometryType.wkbPolygon);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            Geometry lr = new Geometry(wkbGeometryType.wkbLinearRing);
            lr.Vector3(line);
            lr.CloseRings();
            geom.AddGeometryDirectly(lr);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            return _drawFeature(geom, new Feature(new FeatureDefn(null)));
        }

        protected override async Task _draw()
        {
            RecordSet layer = GetMetadata();
            if (layer.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1], layer.Properties.BBox[2], layer.Properties.BBox[3]);
            }
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.features) {
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry poly = feature.GetGeomFieldRef(j);
                        if (poly == null)
                            continue;
                        if (poly.GetGeometryType() == wkbGeometryType.wkbPolygon ||
                            poly.GetGeometryType() == wkbGeometryType.wkbPolygon25D ||
                            poly.GetGeometryType() == wkbGeometryType.wkbPolygonM ||
                            poly.GetGeometryType() == wkbGeometryType.wkbPolygonZM) {
                            if (poly.GetSpatialReference() == null)
                                poly.AssignSpatialReference(GetCrs());
                            await _drawFeatureAsync(poly, feature);
                        } else if (poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygon ||
                            poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygon25D ||
                            poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygonM ||
                            poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygonZM) {
                            int n = poly.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                Geometry poly2 = poly.GetGeometryRef(k);
                                if (poly2.GetSpatialReference() == null)
                                    poly2.AssignSpatialReference(GetCrs());
                                await _drawFeatureAsync(poly2, feature);
                            }
                        }
                        poly.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }

        protected VirgisFeature _drawFeature(Geometry poly,  Feature feature = null)
        {
            Geometry center = poly.Centroid();
            center.AssignSpatialReference(poly.GetSpatialReference());


            //Create the GameObjects
            GameObject dataPoly = Instantiate(PolygonPrefab, center.TransformWorld()[0], Quaternion.identity, transform);



            // add the gis data from geoJSON
            Datapolygon p = dataPoly.GetComponent<Datapolygon>();

            if (feature != null)
                p.feature = feature;

            if (symbology.ContainsKey("body") && symbology["body"].ContainsKey("Label") && symbology["body"].Label != null && (feature?.ContainsKey(symbology["body"].Label) ?? false))
            {
                //Set the label
                GameObject labelObject = Instantiate(LabelPrefab, dataPoly.transform, false);
                labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) * symbology["point"].Transform.Scale.magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string)feature.Get(symbology["body"].Label);
            }


            List<Dataline> polygon = new List<Dataline>();
            List<Geometry> LinearRings = new List<Geometry>();
            for (int i = 0; i < poly.GetGeometryCount(); i++) LinearRings.Add(poly.GetGeometryRef(i));
            // Darw the LinearRing
            foreach (Geometry LinearRing in LinearRings) {
                wkbGeometryType type = LinearRing.GetGeometryType();
                if ( type== wkbGeometryType.wkbLinearRing || type == wkbGeometryType.wkbLineString25D || type == wkbGeometryType.wkbLineString) {
                    GameObject dataLine = Instantiate(LinePrefab, dataPoly.transform, false);
                    Dataline com = dataLine.GetComponent<Dataline>();
                    LinearRing.CloseRings();
                    com.Draw(LinearRing, symbology, LinePrefab, HandlePrefab, null, mainMat, selectedMat, lineMain, lineSelected, true);
                    polygon.Add(com);
                }
            }

            //Draw the Polygon
            p.Draw(polygon, bodyMain);

            return p;
        }

        protected Task<int> _drawFeatureAsync(Geometry poly, Feature feature = null) {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(poly, feature);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override void _checkpoint()
        {
        }
        protected override Task _save()
        {
            Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            foreach (Datapolygon dataFeature in dataFeatures)
            {
                Feature feature = dataFeature.feature;
                Geometry geom = new Geometry(wkbGeometryType.wkbPolygon);
                geom.AssignSpatialReference(AppState.instance.mapProj);
                Dataline[] poly = dataFeature.GetComponentsInChildren<Dataline>();
                foreach (Dataline perimeter in poly) {
                    Geometry lr = new Geometry(wkbGeometryType.wkbLinearRing);
                    lr.Vector3(perimeter.GetVertexPositions());
                    lr.CloseRings();
                    geom.AddGeometryDirectly(lr);
                }
                geom.TransformTo(GetCrs());
                feature.SetGeometryDirectly(geom);
                features.SetFeature(feature);
            }
            features.SyncToDisk();
            return Task.CompletedTask;
        }


        public override GameObject GetFeatureShape()
        {
            GameObject fs = Instantiate(HandlePrefab);
            Datapoint dp = fs.GetComponent<Datapoint>();
            dp.SetMaterial(mainMat, selectedMat);
            return fs;
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
