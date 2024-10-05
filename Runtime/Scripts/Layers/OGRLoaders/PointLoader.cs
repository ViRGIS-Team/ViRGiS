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
using System.Threading.Tasks;
using UnityEngine;
using OSGeo.OGR;
using SpatialReference = OSGeo.OSR.SpatialReference;
using System.Linq;
using System;
using System.Collections;
using VirgisGeometry;

namespace Virgis {

    public class PointLoader : PointLoaderPrototype<Layer> {

        public override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            m_symbology = layer.Units;
            await Load();
        }

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
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
                        string label = "";
                        if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Label") && m_symbology["point"].Label != null && (feature?.ContainsKey(m_symbology["point"].Label) ?? false)) {
                            label = feature.Get<string>(m_symbology["point"].Label);
                        }
                        if (point.GetGeometryType() == wkbGeometryType.wkbPoint ||
                            point.GetGeometryType() == wkbGeometryType.wkbPoint25D ||
                            point.GetGeometryType() == wkbGeometryType.wkbPointM ||
                            point.GetGeometryType() == wkbGeometryType.wkbPointZM) {
                            point
                                .ToVector3d(AppState.instance.mapProj)
                                .ToList()
                                .ForEach(async item => 
                                    await DrawFeatureAsync((Vector3)item, feature.GetFID(), label));
                        } else if
                           (point.GetGeometryType() == wkbGeometryType.wkbMultiPoint ||
                            point.GetGeometryType() == wkbGeometryType.wkbMultiPoint25D ||
                            point.GetGeometryType() == wkbGeometryType.wkbMultiPointM ||
                            point.GetGeometryType() == wkbGeometryType.wkbMultiPointZM) {
                            int n = point.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Label") && m_symbology["point"].Label != null && (feature?.ContainsKey(m_symbology["point"].Label) ?? false)) {
                                    label = feature.Get<string>(m_symbology["point"].Label);
                                } else {
                                    label = "";
                                }
                                Geometry point2 = point.GetGeometryRef(k);
                                point2
                                .ToVector3d(AppState.instance.mapProj)
                                .ToList()
                                .ForEach(async item =>
                                    await DrawFeatureAsync((Vector3) item, feature.GetFID(), label));
                            }
                        }
                        point.Dispose();
                    }
                }
            }
            if (layer.Transform != null) {
                transform.position = AppState.instance.Map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }

        protected override IEnumerator hydrate() {
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            foreach (Datapoint pointFunc in pointFuncs) {
                Feature feature = features.GetFeature(pointFunc.GetFID<long>());
                bool n = false;
                if (feature == null) {
                    feature = new Feature(features.GetLayerDefn());
                    n = true;
                }
                Geometry geom = ((Vector3d)pointFunc.gameObject.transform.position).ToGeometry();
                geom.TransformTo(GetCrs());
                feature.SetGeometryDirectly(geom);
                if (n) {
                    features.CreateFeature(feature);
                } else {
                    features.SetFeature(feature);
                }
                yield return null;
            }
            features.SyncToDisk();
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
    }
}
