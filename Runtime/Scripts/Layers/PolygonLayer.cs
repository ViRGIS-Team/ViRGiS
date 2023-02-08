/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

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

        private GameObject m_handlePrefab;
        private GameObject m_linePrefab;

        private Dictionary<string, Unit> m_symbology;
        private Material m_mainMat;
        private Material m_selectedMat;
        private Material m_lineMain;
        private Material m_lineSelected;
        private Material m_bodyMain;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.POLYGON;
        }


        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
                m_symbology = layer.Properties.Units;

                if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Shape")) {
                    Shapes shape = m_symbology["point"].Shape;
                    switch (shape) {
                        case Shapes.Spheroid:
                            m_handlePrefab = SpherePrefab;
                            break;
                        case Shapes.Cuboid:
                            m_handlePrefab = CubePrefab;
                            break;
                        case Shapes.Cylinder:
                            m_handlePrefab = CylinderPrefab;
                            break;
                        default:
                            m_handlePrefab = SpherePrefab;
                            break;
                    }
                } else {
                    m_handlePrefab = SpherePrefab;
                }

                if (m_symbology.ContainsKey("line") && m_symbology["line"].ContainsKey("Shape")) {
                    Shapes shape = m_symbology["line"].Shape;
                    switch (shape) {
                        case Shapes.Cuboid:
                            m_linePrefab = CuboidLinePrefab;
                            break;
                        case Shapes.Cylinder:
                            m_linePrefab = CylinderLinePrefab;
                            break;
                        default:
                            m_linePrefab = CylinderLinePrefab;
                            break;
                    }
                } else {
                    m_linePrefab = CylinderLinePrefab;
                }

                Color col = m_symbology.ContainsKey("point") ? (Color) m_symbology["point"].Color : Color.white;
                Color sel = m_symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
                Color line = m_symbology.ContainsKey("line") ? (Color) m_symbology["line"].Color : Color.white;
                Color lineSel = m_symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
                Color body = m_symbology.ContainsKey("body") ? (Color) m_symbology["body"].Color : Color.white;
                m_mainMat = Instantiate(PointBaseMaterial);
                m_mainMat.SetColor("_BaseColor", col);
                m_selectedMat = Instantiate(PointBaseMaterial);
                m_selectedMat.SetColor("_BaseColor", sel);
                m_lineMain = Instantiate(LineBaseMaterial);
                m_lineMain.SetColor("_BaseColor", line);
                m_lineSelected = Instantiate(LineBaseMaterial);
                m_lineSelected.SetColor("_BaseColor", lineSel);
                m_bodyMain = Instantiate(BodyBaseMaterial);
                m_bodyMain.SetColor("_BaseColor", body);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected VirgisFeature _addFeature(Vector3[] line)
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

            if (m_symbology.ContainsKey("body") && m_symbology["body"].ContainsKey("Label") && m_symbology["body"].Label != null && (feature?.ContainsKey(m_symbology["body"].Label) ?? false))
            {
                //Set the label
                GameObject labelObject = Instantiate(LabelPrefab, dataPoly.transform, false);
                labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) * m_symbology["point"].Transform.Scale.magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string)feature.Get(m_symbology["body"].Label);
            }


            List<Dataline> polygon = new List<Dataline>();
            List<Geometry> LinearRings = new List<Geometry>();
            for (int i = 0; i < poly.GetGeometryCount(); i++) LinearRings.Add(poly.GetGeometryRef(i));
            // Darw the LinearRing
            foreach (Geometry LinearRing in LinearRings) {
                wkbGeometryType type = LinearRing.GetGeometryType();
                if ( type== wkbGeometryType.wkbLinearRing || type == wkbGeometryType.wkbLineString25D || type == wkbGeometryType.wkbLineString) {
                    GameObject dataLine = Instantiate(m_linePrefab, dataPoly.transform, false);
                    Dataline com = dataLine.GetComponent<Dataline>();
                    LinearRing.CloseRings();
                    com.Draw(LinearRing, m_symbology, m_handlePrefab, null, m_mainMat, m_selectedMat, m_lineMain, m_lineSelected, true);
                    polygon.Add(com);
                }
            }

            //Draw the Polygon
            p.Draw(polygon, m_bodyMain);

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
            GameObject fs = Instantiate(m_handlePrefab);
            Datapoint dp = fs.GetComponent<Datapoint>();
            dp.SetMaterial(m_mainMat, m_selectedMat);
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
