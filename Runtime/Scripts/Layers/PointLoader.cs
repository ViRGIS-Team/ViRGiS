/* MIT License

Copyright (c) 2020 - 23 Runette Software

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
using SpatialReference = OSGeo.OSR.SpatialReference;
using System.Linq;

namespace Virgis {

    public class PointLoader : VirgisLoader<Layer> {

        private GameObject m_pointPrefab;
        private Dictionary<string, Unit> m_symbology;
        private float m_displacement;
        private PointLayer parent;

        public override async Task _init() {
            parent = m_parent as PointLayer;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
        }

        protected Task<int> Load() {
            RecordSet layer = _layer as RecordSet;
            m_symbology = layer.Units;
            m_displacement = 1.0f;
            if (m_symbology.ContainsKey("point") &&
                m_symbology["point"].ContainsKey("Shape")) {
                Shapes shape = m_symbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        m_pointPrefab = parent.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        m_pointPrefab = parent.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_pointPrefab = parent.CylinderPrefab;
                        m_displacement = 1.5f;
                        break;
                    default:
                        m_pointPrefab = parent.SpherePrefab;
                        break;
                }
            } else {
                m_pointPrefab = parent.SpherePrefab;
            }

            Color col = m_symbology.ContainsKey("point") ? 
                (Color) m_symbology["point"].Color : Color.white;
            Color sel = m_symbology.ContainsKey("point") ? 
                new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            parent.SetMaterial(col);
            parent.SetMaterial(sel);
            return Task.FromResult(0);
        }

        protected VirgisFeature _addFeature(Vector3[] geometry) {
            VirgisFeature newFeature = _drawFeature(geometry[0], new Feature(new FeatureDefn(null)));
            changed = true;
            return newFeature;
        }

        public override async Task _draw() {
            RecordSet layer = GetMetadata() as RecordSet;
            if (layer.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], 
                    layer.Properties.BBox[1], layer.Properties.BBox[2], 
                    layer.Properties.BBox[3]);
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
            GameObject dataPoint = Instantiate(m_pointPrefab, transform, false);
            Datapoint com = dataPoint.GetComponent<Datapoint>();
            com.Spawn(transform);

            // add the gis data from source
            dataPoint.transform.position = position;
            if (feature != null) com.feature = feature;

            //Set the symbology
            if (m_symbology.ContainsKey("point")) {
                dataPoint.transform.localScale = m_symbology["point"].Transform.Scale;
                dataPoint.transform.localRotation = m_symbology["point"].Transform.Rotate;
                dataPoint.transform.Translate(m_symbology["point"].Transform.Position, Space.Self);
            }


            //Set the label
            if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Label") && m_symbology["point"].Label != null && (feature?.ContainsKey(m_symbology["point"].Label) ?? false)) {
                GameObject labelObject = Instantiate(parent.LabelPrefab, 
                                                     dataPoint.transform, false
                                                     );
                labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                labelObject.transform.localPosition = Vector3.up * m_displacement;
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string) feature.Get(m_symbology["point"].Label);
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

        public override void _checkpoint() {
        }
        public override Task _save() {
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            List<Feature> thisFeatures = new List<Feature>();
            long n = features.GetFeatureCount(0);
            for (int i = 0; i < (int) n; i++) features.DeleteFeature(i);
            foreach (Datapoint pointFunc in pointFuncs) {
                Feature feature = pointFunc.feature as Feature;
                Geometry geom = (pointFunc.gameObject.transform.position.ToGeometry());
                geom.TransformTo(GetCrs());
                feature.SetGeometryDirectly(geom);
                features.CreateFeature(feature);
            }
            features.SyncToDisk();
            return Task.CompletedTask;
        }

        public override GameObject GetFeatureShape() {
            GameObject fs = Instantiate(m_pointPrefab, parent.transform);
            Datapoint com = fs.GetComponent<Datapoint>();
            return fs;
        }

        public void RemoveVertex(VirgisFeature vertex) {
            if (AppState.instance.InEditSession() && IsEditable()) {
                Destroy(vertex.gameObject);
            }
        }
    }
}
