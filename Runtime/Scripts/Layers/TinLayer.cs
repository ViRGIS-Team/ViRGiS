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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Project;
using OSGeo.OGR;
using System.Threading.Tasks;
using g3;

namespace Virgis {


    public class TinLayer : VirgisLayer<RecordSet, Layer> {

        // The prefab for the data points to be instantiated
        public GameObject MeshPrefab; // Prefab to be used for the polygons
        public GameObject LabelPrefab; // Prefab to used for the Labels
        public Material MeshBaseMaterial;
        public Material WireframeMaterial;


        private Material m_bodyMain;
        private Dictionary<string, Unit> m_symbology;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.MESH;
        }

        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
                m_symbology = layer.Properties.Units;
                Color body = m_symbology.ContainsKey("body") ? (Color) m_symbology["body"].Color : Color.white;
                m_bodyMain = Instantiate(MeshBaseMaterial);
                m_bodyMain.SetColor("_BaseColor", body);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected override VirgisFeature _addFeature(Vector3[] line) {
            throw new System.NotImplementedException();
        }

        protected async override Task _draw() {
            RecordSet layer = GetMetadata();
            if (layer.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1], layer.Properties.BBox[2], layer.Properties.BBox[3]);
            }
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.features) {
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry tin = feature.GetGeomFieldRef(j);
                        if (tin == null)
                            continue;
                        if (tin.GetGeometryType() == wkbGeometryType.wkbTIN ||
                            tin.GetGeometryType() == wkbGeometryType.wkbTINZ ||
                            tin.GetGeometryType() == wkbGeometryType.wkbTINM ||
                            tin.GetGeometryType() == wkbGeometryType.wkbTINZM) {
                            if (tin.GetSpatialReference() == null)
                                tin.AssignSpatialReference(GetCrs());
                            await _drawFeatureAsync(tin, feature);
                        }
                        tin.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }

        protected Task<int> _drawFeatureAsync(Geometry tin, Feature feature = null) {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(tin, feature);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        protected VirgisFeature _drawFeature(Geometry tin, Feature feature = null) {
            
            //Create the GameObjects
            GameObject dataTIN = Instantiate(MeshPrefab, transform);

            EditableMesh mesh = dataTIN.GetComponent<EditableMesh>();

            if (feature != null)
                mesh.feature = feature;

            List<Geometry> trigeos = new List<Geometry>();
            List<Vector3d> trivects = new List<Vector3d>();
            List<int> tris = new List<int>();

            for (int i = 0; i < tin.GetGeometryCount(); i++) {
                trigeos.Add(tin.GetGeometryRef(i));
            }

            HashSet<Vector3d> vertexhash = new HashSet<Vector3d>();

            for (int i = 0; i < trigeos.Count; i++) {
                Geometry tri = trigeos[i];
                Geometry linearring = tri.GetGeometryRef(0);
                for (int j = 0; j < 3; j++) {
                    double[] argout = new double[3];
                    linearring.GetPoint(j, argout);
                    Vector3d vertex = new Vector3d(argout);
                    vertexhash.Add(vertex);
                    trivects.Add(vertex);
                }
                tri.Dispose();
                linearring.Dispose();
            }

            List<Vector3d> vertexes = vertexhash.ToList();

            foreach (Vector3d vertex in trivects) {
                tris.Add(vertexes.IndexOf(vertex));
            }

            DMesh3 dmesh = DMesh3Builder.Build<Vector3d, int, int>(vertexes, tris);
            string crs;
            tin.GetSpatialReference().ExportToWkt(out crs, null);
            dmesh.AttachMetadata("CRS", crs );

            mesh.Draw(dmesh, m_bodyMain, WireframeMaterial, true);

            //if (symbology.ContainsKey("body") && symbology["body"].ContainsKey("Label") && symbology["body"].Label != null && (feature?.ContainsKey(symbology["body"].Label) ?? false)) {
            //    //Set the label
            //    GameObject labelObject = Instantiate(LabelPrefab, dataPoly.transform, false);
            //    labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) * symbology["point"].Transform.Scale.magnitude, Space.Self);
            //    Text labelText = labelObject.GetComponentInChildren<Text>();
            //    labelText.text = (string) feature.Get(symbology["body"].Label);
            //}

            return mesh;

        }

        protected override void _checkpoint() {

        }

        protected override Task _save() {
            throw new System.NotImplementedException();
        }
    }
}