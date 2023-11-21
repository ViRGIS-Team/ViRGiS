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
using SpatialReference = OSGeo.OSR.SpatialReference;
using g3;
using System.Linq;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLoader : VirgisLoader<Layer>
    {
        private GameObject m_handlePrefab;
        private GameObject m_linePrefab;

        private Dictionary<string, Unit> m_symbology;
        private PolygonLayer parent;

        public override async Task _init() {
            parent = m_parent as PolygonLayer;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
        }

        protected Task<int> Load() {
            RecordSet layer = _layer as RecordSet;
            m_symbology = layer.Properties.Units;

            if (m_symbology.ContainsKey("point") &&
                m_symbology["point"].ContainsKey("Shape")) {
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

            if (m_symbology.ContainsKey("line") && 
                m_symbology["line"].ContainsKey("Shape")) {
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

            Color col = m_symbology.ContainsKey("point") ? 
                (Color) m_symbology["point"].Color : Color.white;
            Color sel = m_symbology.ContainsKey("point") ?
                new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            Color line = m_symbology.ContainsKey("line") ? 
                (Color) m_symbology["line"].Color : Color.white;
            Color lineSel = m_symbology.ContainsKey("line") ? 
                new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
            Color body = m_symbology.ContainsKey("body") ? 
                (Color) m_symbology["body"].Color : Color.white;
            parent.SetMaterial(col);
            parent.SetMaterial(sel);
            parent.SetMaterial(line);
            parent.SetMaterial(lineSel);
            parent.SetMaterial(body);
            return Task.FromResult(1);
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

        public override async Task _draw()
        {
            RecordSet layer = GetMetadata() as RecordSet;
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
            GameObject dataPoly = Instantiate(parent.PolygonPrefab, center.TransformWorld()[0], Quaternion.identity, transform);



            Datapolygon p = dataPoly.GetComponent<Datapolygon>();
            p.Spawn(transform);

            if (feature != null)
                p.feature = feature;

            if (m_symbology.ContainsKey("body") && m_symbology["body"].ContainsKey("Label") && 
                    m_symbology["body"].Label != null && (feature?.ContainsKey(m_symbology["body"].Label
                ) ?? false))
            {
                //Set the label
                GameObject labelObject = Instantiate(parent.LabelPrefab, dataPoly.transform, false);
                labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) * 
                                                m_symbology["point"].Transform.Scale.magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string)feature.Get(m_symbology["body"].Label);
            }


            List<Dataline> polygon = new List<Dataline>();
            List<Geometry> LinearRings = new List<Geometry>();
            for (int i = 0; i < poly.GetGeometryCount(); i++) LinearRings.Add(poly.GetGeometryRef(i));
            // Draw the LinearRing
            foreach (Geometry LinearRing in LinearRings) {
                wkbGeometryType type = LinearRing.GetGeometryType();
                if ( type== wkbGeometryType.wkbLinearRing || 
                            type == wkbGeometryType.wkbLineString25D || 
                            type == wkbGeometryType.wkbLineString
                    ) {
                    GameObject dataLine = Instantiate(m_linePrefab, dataPoly.transform, false);
                    Dataline com = dataLine.GetComponent<Dataline>();
                    com.Spawn(dataPoly.transform);
                    LinearRing.CloseRings();
                    DCurve3 curve = new DCurve3();
                    curve.FromGeometry(LinearRing);
                    com.Draw(curve,
                        m_symbology.ToDictionary(
                            item => item.Key,
                            item => item.Value as UnitPrototype
                        ), 
                        m_handlePrefab, 
                        null,
                        true
                    );
                    polygon.Add(com);
                }
            }

            //Draw the Polygon
            p.Draw(polygon);

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

        public override void _checkpoint()
        {
        }
        public override Task _save()
        {
            Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            foreach (Datapolygon dataFeature in dataFeatures)
            {
                Feature feature = dataFeature.feature as Feature;
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
            GameObject fs = Instantiate(m_handlePrefab, parent.transform);
            Datapoint dp = fs.GetComponent<Datapoint>();
            dp.Spawn(parent.transform);
            dp.SetMaterial(0);
            return fs;
        }
    }
}
