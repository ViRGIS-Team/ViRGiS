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
using SpatialReference = OSGeo.OSR.SpatialReference;
using System.Threading.Tasks;
using g3;

namespace Virgis {


    public class TinLoader : VirgisLoader<Layer> {

        private Dictionary<string, Unit> m_symbology;
        private TinLayer parent;

        public override async Task _init() {
            parent = m_parent as TinLayer;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
        }

        protected Task<int> Load() {
            Task<int> t1 = new Task<int>(() => {
                RecordSet layer = _layer as RecordSet;
                m_symbology = layer.Units;
                foreach (string key in m_symbology.Keys) {
                    Unit unit = m_symbology[key];
                    SerializableMaterialHash hash = new() {
                        Name = key,
                        Color = unit.Color,
                    };
                    m_materials.Add(key, hash);
                }
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        public async override Task _draw() {
            RecordSet layer = GetMetadata() as RecordSet;
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
                        else if (tin.GetGeometryType() == wkbGeometryType.wkbPolyhedralSurface ||
                            tin.GetGeometryType() == wkbGeometryType.wkbPolyhedralSurfaceZ ||
                            tin.GetGeometryType() == wkbGeometryType.wkbPolyhedralSurfaceM ||
                            tin.GetGeometryType() == wkbGeometryType.wkbPolyhedralSurfaceZM) {
                            if (tin.GetSpatialReference() == null)
                                tin.AssignSpatialReference(GetCrs());
                            await _drawFeatureAsync(tin, feature);
                        }
                        tin.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.Map.transform.TransformPoint(layer.Transform.Position);
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
            GameObject dataTIN = Instantiate(parent.MeshPrefab, transform);

            EditableMesh mesh = dataTIN.GetComponent<EditableMesh>();

            List<Geometry> trigeos = new List<Geometry>();
            List<Vector3d> trivects = new List<Vector3d>();
            List<int> tris = new List<int>();

            for (int i = 0; i < tin.GetGeometryCount(); i++) {
                trigeos.Add(tin.GetGeometryRef(i));
            }

            HashSet<Vector3d> vertexhash = new HashSet<Vector3d>();
            double[] argout = new double[3];
            Vector3d vertex;
            Vector3d vertex0;
            Vector3d lastvertex;
            for (int i = 0; i < trigeos.Count; i++) {
                Geometry tri = trigeos[i];
                Geometry linearring = tri.GetGeometryRef(0);
                int points = linearring.GetPointCount();
                linearring.GetPoint(0, argout);
                vertex0 = new Vector3d(argout);
                vertexhash.Add(vertex0);
                linearring.GetPoint(1, argout);
                lastvertex = new Vector3d(argout);
                vertexhash.Add(lastvertex);
                for (int j = 2; j < points - 1; j++) {
                    linearring.GetPoint(j, argout);
                    vertex = new Vector3d(argout);
                    vertexhash.Add(vertex);
                    trivects.Add(vertex0);
                    trivects.Add(lastvertex);
                    trivects.Add(vertex);
                    lastvertex = vertex;
                }
                tri.Dispose();
                linearring.Dispose();
            }

            List<Vector3d> vertexes = vertexhash.ToList();

            foreach (Vector3d vert in trivects) {
                tris.Add(vertexes.IndexOf(vert));
            }

            DMesh3 dmesh = DMesh3Builder.Build<Vector3d, int, int>(vertexes, tris);
            string crs;
            tin.GetSpatialReference().ExportToWkt(out crs, null);
            dmesh.AttachMetadata("CRS", crs );
            dmesh.Transform();

            Unit body;
            if (!m_symbology.TryGetValue("body", out body)) body = new();
            mesh.Draw(dmesh, body );

            //if (symbology.ContainsKey("body") && symbology["body"].ContainsKey("Label") && symbology["body"].Label != null && (feature?.ContainsKey(symbology["body"].Label) ?? false)) {
            //    //Set the label
            //    GameObject labelObject = Instantiate(LabelPrefab, dataPoly.transform, false);
            //    labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) * symbology["point"].Transform.Scale.magnitude, Space.Self);
            //    Text labelText = labelObject.GetComponentInChildren<Text>();
            //    labelText.text = (string) feature.Get(symbology["body"].Label);
            //}

            return mesh;

        }

        public override void _checkpoint() {

        }

        public override Task _save() {
            throw new System.NotImplementedException();
        }
    }
}