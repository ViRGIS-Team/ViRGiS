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

using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using Project;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using g3;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLoader : VirgisLoader<Layer>
    {
        private GameObject m_handlePrefab;
        private GameObject m_linePrefab;
        private Dictionary<string, Unit> m_symbology;
        private LineLayer parent;

        public override async Task _init() {
            parent = m_parent as LineLayer;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
        }

        protected Task<int> Load() {
            RecordSet layer = _layer as RecordSet;
            m_symbology = layer.Units;

            if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Shape")) {
                Shapes shape = m_symbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        m_handlePrefab = parent.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        m_handlePrefab = parent.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_handlePrefab = parent.CylinderPrefab;
                        break;
                    default:
                        m_handlePrefab = parent.SpherePrefab;
                        break;
                }
            } else {
                m_handlePrefab = parent.SpherePrefab;
            }

            if (m_symbology.ContainsKey("line") && m_symbology["line"].ContainsKey("Shape")) {
                Shapes shape = m_symbology["line"].Shape;
                switch (shape) {
                    case Shapes.Cuboid:
                        m_linePrefab = parent.CuboidLinePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_linePrefab = parent.CylinderLinePrefab;
                        break;
                    default:
                        m_linePrefab = parent.CylinderLinePrefab;
                        break;
                }
            } else {
                m_linePrefab = parent.CylinderLinePrefab;
            }

            Color col = m_symbology.ContainsKey("point") ? (Color) m_symbology["point"].Color : Color.white;
            Color sel = m_symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            Color line = m_symbology.ContainsKey("line") ? (Color) m_symbology["line"].Color : Color.white;
            Color lineSel = m_symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
            parent.SetMaterial("point",col);
            parent.SetMaterial("point_sel",sel);
            parent.SetMaterial("line", line);
            parent.SetMaterial("line_sel",lineSel);
            return Task.FromResult(1);
        }

        protected VirgisFeature _addFeature(Vector3[] line)
        {
            Geometry geom = new Geometry(wkbGeometryType.wkbLineString25D);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            geom.Vector3(line);
            return _drawFeature(geom, new Feature(new FeatureDefn(null)));
        }

        public override async Task _draw()
        {
            RecordSet layer = GetMetadata()as RecordSet;
            if (layer.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1], layer.Properties.BBox[2], layer.Properties.BBox[3]);
            }
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.features) {
                    if (feature == null)
                        continue;
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry line = feature.GetGeomFieldRef(j);
                        if (line == null)
                            continue;
                        if (line.GetGeometryType() == wkbGeometryType.wkbLineString ||
                            line.GetGeometryType() == wkbGeometryType.wkbLineString25D ||
                            line.GetGeometryType() == wkbGeometryType.wkbLineStringM ||
                            line.GetGeometryType() == wkbGeometryType.wkbLineStringZM
                        ) {
                            if (line.GetSpatialReference() == null)
                                line.AssignSpatialReference(GetCrs());
                            await _drawFeatureAsync(line, feature);
                        } else if
                            (line.GetGeometryType() == wkbGeometryType.wkbMultiLineString ||
                            line.GetGeometryType() == wkbGeometryType.wkbMultiLineString25D ||
                            line.GetGeometryType() == wkbGeometryType.wkbMultiLineStringM ||
                            line.GetGeometryType() == wkbGeometryType.wkbMultiLineStringZM
                         ) {
                            int n = line.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                Geometry Line2 = line.GetGeometryRef(k);
                                if (Line2.GetSpatialReference() == null)
                                    Line2.AssignSpatialReference(GetCrs());
                                await _drawFeatureAsync(Line2, feature);
                            }
                        }
                        line.Dispose();
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
        /// Draws a single feature based on world scale coordinates
        /// </summary>
        /// <param name="line"> Geometry</param>
        /// <param name="feature">Feature (optinal)</param>
        protected VirgisFeature _drawFeature(Geometry line, Feature feature = null)
        {
            GameObject dataLine = Instantiate(m_linePrefab, transform, false);

            //set the gisProject properties
            Dataline com = dataLine.GetComponent<Dataline>();
            com.Spawn(transform);

            if (feature != null)
                com.feature = feature;

            //DEBUG
            line.ExportToIsoWkt(out string wkt);

            //Draw the line
            DCurve3 curve = new();
            curve.FromGeometry(line);
            com.Draw(curve, 
                m_symbology.ToDictionary(
                    item => item.Key,
                item => item.Value as UnitPrototype
                ), 
                m_handlePrefab, 
                parent.LabelPrefab,
                line.IsRing()
            );

            return com;
        }

        protected Task<int> _drawFeatureAsync(Geometry line, Feature feature = null) {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(line, feature);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        public override void _checkpoint()
        {
        }

        public override Task _save()
        {
            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            foreach (Dataline dataFeature in dataFeatures) {
                Feature feature = dataFeature.feature as Feature;
                Geometry geom = new Geometry(wkbGeometryType.wkbLineString25D);
                geom.AssignSpatialReference(AppState.instance.mapProj);
                geom.Vector3(dataFeature.GetVertexPositions());
                geom.TransformTo(GetCrs());
                feature.SetGeometryDirectly(geom);
                features.SetFeature(feature);
            };
            features.SyncToDisk();
            return Task.CompletedTask;

        }

        public override GameObject GetFeatureShape()
        {
            GameObject fs = Instantiate(m_handlePrefab, parent.transform);
            Datapoint com = fs.GetComponent<Datapoint>();
            com.Spawn(parent.transform);
            //com.SetMaterial(0);
            return fs;
        }
    }
}
