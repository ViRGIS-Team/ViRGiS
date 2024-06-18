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
using System;
using UnityEngine;
using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using VirgisGeometry;
using System.Collections;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLoader : PolygonLoaderPrototype<Layer>
    {

        public override async Task _init() {
            m_symbology = (GetMetadata() as RecordSet).Units;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
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
                            await _drawPoly(poly, feature);
                        } else if (poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygon ||
                            poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygon25D ||
                            poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygonM ||
                            poly.GetGeometryType() == wkbGeometryType.wkbMultiPolygonZM) {
                            int n = poly.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                Geometry poly2 = poly.GetGeometryRef(k);
                                if (poly2.GetSpatialReference() == null)
                                    poly2.AssignSpatialReference(GetCrs());
                                await _drawPoly(poly2, feature);
                            }
                        }
                        poly.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.Map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }

        protected async Task _drawPoly(Geometry poly,  Feature feature)
        {
            string label = "";
            if (m_symbology.ContainsKey("body") && m_symbology["body"].ContainsKey("Label") && 
                    m_symbology["body"].Label != null && (feature?.ContainsKey(m_symbology["body"].Label
                ) ?? false))
            {
                label = (string)feature.Get(m_symbology["body"].Label);
            }

            // Get the linear rings as Dcurve3
            List<DCurve3> polygon = new();
            List<Geometry> LinearRings = new();
            for (int i = 0; i < poly.GetGeometryCount(); i++) 
                LinearRings.Add(poly.GetGeometryRef(i));
            foreach (Geometry LinearRing in LinearRings) {
                wkbGeometryType type = LinearRing.GetGeometryType();
                if ( type== wkbGeometryType.wkbLinearRing || 
                            type == wkbGeometryType.wkbLineString25D || 
                            type == wkbGeometryType.wkbLineString
                    ) {
                    LinearRing.CloseRings();
                    DCurve3 curve = new DCurve3();
                    curve.FromGeometry(LinearRing);
                    curve.Closed = true;
                    polygon.Add(curve);
                }
            }

            //Draw the Polygon
            await _drawFeatureAsync(polygon, feature.GetFID());
        }

        protected override object GetNextFID() {
            features.ResetReading();
            long highest = 0;
            while (true) {
                Feature feature = features.GetNextFeature();
                if (feature == null)
                    break;
                long fid = feature.GetFID();
                highest = Math.Max(fid, highest);
            }
            return highest + 1;
        }

        protected override IEnumerator hydrate() {
            Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            foreach (Datapolygon dataFeature in dataFeatures) {
                //Feature feature = dataFeature.feature as Feature;
                //Geometry geom = new Geometry(wkbGeometryType.wkbPolygon);
                //geom.AssignSpatialReference(AppState.instance.mapProj);
                //Dataline[] poly = dataFeature.GetComponentsInChildren<Dataline>();
                //foreach (Dataline perimeter in poly) {
                //    Geometry lr = new Geometry(wkbGeometryType.wkbLinearRing);
                //    lr.Vector3(perimeter.GetVertexPositions());
                //    lr.CloseRings();
                //    geom.AddGeometryDirectly(lr);
                //}
                //geom.TransformTo(GetCrs());
                //feature.SetGeometryDirectly(geom);
                //features.SetFeature(feature);
                yield return null;
            }
            features.SyncToDisk();
        }
    }
}
