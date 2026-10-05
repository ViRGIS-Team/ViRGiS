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
        
        private wkbGeometryType _mType;

        public override Task _init() {
            RecordSet layer = Layer as RecordSet;
            DataUnit = new() { Representation = DataUnitRepresent.Points };
            MSymbology = layer?.Units.ToDictionary(x => x.Key, x => (UnitPrototype)x.Value);
            ReadSymbology();
            return Task.CompletedTask;
        }

        public SpatialReference GetCrs() {
            return MCrs as SpatialReference;
        }

        public override async Task _draw() {
            RecordSet layer = GetMetadata() as RecordSet;
            if (layer?.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], 
                    layer.Properties.BBox[1], layer.Properties.BBox[2], 
                    layer.Properties.BBox[3]);
            }
            SetCrs(OgrReader.getSR(features, layer));
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.Features) {
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry point = feature.GetGeomFieldRef(j);
                        _mType = point.GetGeometryType();
                        string label = "";
                        if (MSymbology.ContainsKey("point") && MSymbology["point"].ContainsKey("Label") && MSymbology["point"].Label != null && (feature.ContainsKey(MSymbology["point"].Label))) {
                            label = feature.Get<string>(MSymbology["point"].Label);
                        }
                        if (_mType == wkbGeometryType.wkbPoint ||
                            _mType == wkbGeometryType.wkbPoint25D ||
                            _mType == wkbGeometryType.wkbPointM ||
                            _mType == wkbGeometryType.wkbPointZM) {
                            point
                                .ToVector3d(AppState.Instance.MapProj)
                                .ToList()
                                .ForEach(async item => 
                                    await DrawFeatureAsync((Vector3)item, feature.GetFID(), 0, label));
                        } else if
                           (_mType == wkbGeometryType.wkbMultiPoint ||
                            _mType == wkbGeometryType.wkbMultiPoint25D ||
                            _mType == wkbGeometryType.wkbMultiPointM ||
                            _mType == wkbGeometryType.wkbMultiPointZM) {
                            int n = point.GetGeometryCount();
                            for (int k = 0; k < n; k++) {
                                if (MSymbology.ContainsKey("point") && MSymbology["point"].ContainsKey("Label") && MSymbology["point"].Label != null && (feature.ContainsKey(MSymbology["point"].Label))) {
                                    label = feature.Get<string>(MSymbology["point"].Label);
                                } else {
                                    label = "";
                                }
                                Geometry point2 = point.GetGeometryRef(k);
                                point2
                                .ToVector3d(AppState.Instance.MapProj)
                                .ToList()
                                .ForEach(async item =>
                                    await DrawFeatureAsync((Vector3) item, feature.GetFID(), k, label));
                            }
                        }
                        point.Dispose();
                    }
                }
            }
            if (layer?.Transform != null) {
                transform.position = AppState.Instance.Map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
        }

        protected override IEnumerator Hydrate() {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            foreach (Datapoint pointFunc in pointFuncs) {
                try {
                    if (!pointFunc.changed) continue;
                    Feature feature = features.GetFeature(pointFunc.GetFid<long>());
                    bool n = false;
                    if (feature == null) {
                        feature = new Feature(features.GetLayerDefn());
                        n = true;
                    }

                    if (feature.GetDefnRef().GetGeomFieldCount() > 1)
                        throw new NotImplementedException("Save is not supported on this type of Geometry");
                    Vector3d pos = pointFunc.gameObject.transform.localPosition;
                    Geometry geom;

                    switch (_mType) {
                        case wkbGeometryType.wkbPoint:
                        case wkbGeometryType.wkbPointM:
                        case wkbGeometryType.wkbPointZM:
                        case wkbGeometryType.wkbPoint25D:
                            geom = pos.ToGeometry(_mType);
                            geom.TransformTo(GetCrs());
                            feature.SetGeometryDirectly(geom);
                            break;
                        case wkbGeometryType.wkbMultiPoint:
                        case wkbGeometryType.wkbMultiPoint25D:
                        case wkbGeometryType.wkbMultiPointM:
                        case wkbGeometryType.wkbMultiPointZM:
                            Geometry parentGeom = feature.GetGeometryRef();
                            wkbGeometryType type;
                            if (parentGeom.GetGeometryCount() > 0) {
                                type = parentGeom.GetGeometryRef(0).GetGeometryType();
                            } else {
                                type = wkbGeometryType.wkbMultiPoint;
                            }

                            parentGeom.RemoveGeometry(pointFunc.GetGid<int>());

                            geom = pos.ToGeometry(type);
                            geom.TransformTo(GetCrs());
                            parentGeom.AddGeometryDirectly(geom);
                            parentGeom.Dispose();
                            break;
                        default:
                            throw new NotImplementedException("Save is not supported on this type of Geometry");
                    }

                    if (n) {
                        features.CreateFeature(feature);
                    } else {
                        features.SetFeature(feature);
                    }

                    features.Dispose();
                    if (watch.ElapsedMilliseconds < 100) continue;
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                
                yield return null;
                watch.Restart();
            }
            features.SyncToDisk();
        }

        protected override object GetNextFid() {
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
