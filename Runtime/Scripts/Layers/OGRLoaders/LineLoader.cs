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

using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using Project;
using System.Threading.Tasks;
using System.Collections;
using System;
using UnityEngine;
using g3;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLoader : LineLoaderPrototype<Layer>
    {
        public override async Task _init() {
            parent = m_parent as LineLayer;
            m_symbology = (GetMetadata() as RecordSet).Units;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
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
                            DCurve3 curve = new();
                            curve.FromGeometry(line);
                            await _drawFeatureAsync(curve, feature.GetFID());
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
                                DCurve3 curve = new();
                                curve.FromGeometry(Line2);
                                await _drawFeatureAsync(curve, feature.GetFID());
                            }
                        }
                        line.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.Map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
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


        protected override IEnumerator hydrate()
        {
            /*            Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
                        foreach (Dataline dataFeature in dataFeatures) {
                            Feature feature = dataFeature.feature as Feature;
                            Geometry geom = new Geometry(wkbGeometryType.wkbLineString25D);
                            geom.AssignSpatialReference(AppState.instance.mapProj);
                            geom.Vector3(dataFeature.GetVertexPositions());
                            geom.TransformTo(GetCrs());
                            feature.SetGeometryDirectly(geom);
                            features.SetFeature(feature);
                        };
                        features.SyncToDisk();*/
            return null;
        }
    }
}
