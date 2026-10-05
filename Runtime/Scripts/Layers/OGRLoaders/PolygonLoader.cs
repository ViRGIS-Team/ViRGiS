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
using System.Linq;

namespace Virgis {

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLoader : PolygonLoaderPrototype<Layer> {
        private wkbGeometryType _mType;

        public override Task _init() {
            RecordSet layer = Layer as RecordSet;
            DataUnit = new() { Representation = DataUnitRepresent.Area };
            MSymbology = layer?.Units.ToDictionary(x => x.Key, x => (UnitPrototype) x.Value);
            ReadSymbology();
            return Task.CompletedTask;
        }

        public SpatialReference GetCrs() {
            return MCrs as SpatialReference;
        }

        public override async Task _draw() {
            try {
                RecordSet layer = GetMetadata() as RecordSet;
                if (layer?.Properties.BBox != null) {
                    features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1],
                        layer.Properties.BBox[2], layer.Properties.BBox[3]);
                }

                using (OgrReader ogrReader = new OgrReader()) {
                    await ogrReader.GetFeaturesAsync(features);
                    foreach (Feature feature in ogrReader.Features) {
                        int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                        for (int j = 0; j < geoCount; j++) {
                            Geometry poly = feature.GetGeomFieldRef(j);
                            if (poly == null)
                                continue;
                            _mType = poly.GetGeometryType();
                            switch (_mType) {
                                case wkbGeometryType.wkbPolygon:
                                case wkbGeometryType.wkbPolygon25D:
                                case wkbGeometryType.wkbPolygonM:
                                case wkbGeometryType.wkbPolygonZM:
                                    if (poly.GetSpatialReference() == null)
                                        poly.AssignSpatialReference(GetCrs());
                                    await DrawPoly(poly, feature, 0);
                                    break;
                                case wkbGeometryType.wkbMultiPolygon:
                                case wkbGeometryType.wkbMultiPolygon25D:
                                case wkbGeometryType.wkbMultiPolygonM:
                                case wkbGeometryType.wkbMultiPolygonZM:
                                    int n = poly.GetGeometryCount();
                                    for (int k = 0; k < n; k++) {
                                        Geometry poly2 = poly.GetGeometryRef(k);
                                        if (poly2.GetSpatialReference() == null)
                                            poly2.AssignSpatialReference(GetCrs());
                                        await DrawPoly(poly2, feature, k);
                                    }

                                    break;
                                default:
                                    throw new Exception("Layer Type Fault");
                            }

                            poly.Dispose();
                        }
                    }
                }

                if (layer?.Transform != null) {
                    transform.position = AppState.Instance.Map.transform.TransformPoint(layer.Transform.Position);
                    transform.rotation = layer.Transform.Rotate;
                    transform.localScale = layer.Transform.Scale;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private async Task DrawPoly(Geometry poly, Feature feature, int gid) {
            string label;
            if (MSymbology.ContainsKey("body") && MSymbology["body"].ContainsKey("Label") &&
                MSymbology["body"].Label != null && (feature?.ContainsKey(MSymbology["body"].Label
                ) ?? false)) {
                label = feature.Get<string>(MSymbology["body"].Label);
            }

            // Get the linear rings as Dcurve3
            List<DCurve3> polygon = new();
            for (int i = 0; i < poly.GetGeometryCount(); i++) {
                Geometry linearRing = poly.GetGeometryRef(i);
                wkbGeometryType type = linearRing.GetGeometryType();
                if (type == wkbGeometryType.wkbLinearRing ||
                    type == wkbGeometryType.wkbLineString25D ||
                    type == wkbGeometryType.wkbLineString
                   ) {
                    linearRing.CloseRings();
                    DCurve3 curve = linearRing.ToCurve(
                        AppState.Instance.MapProj
                    );
                    curve.Closed = true;
                    polygon.Add(curve);
                }

                linearRing.Dispose();
            }

            //Draw the Polygon
            await _drawFeatureAsync(polygon, feature?.GetFID(), gid);
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

        protected override IEnumerator Hydrate() {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            Datapolygon[] polyFuncs = gameObject.GetComponentsInChildren<Datapolygon>();
            foreach (Datapolygon polyFunc in polyFuncs) {
                try {
                    if (!polyFunc.changed) continue;
                    Feature feature = features.GetFeature(polyFunc.GetFid<long>());
                    bool n = false;
                    if (feature == null) {
                        feature = new Feature(features.GetLayerDefn());
                        n = true;
                    }

                    if (feature.GetDefnRef().GetGeomFieldCount() > 1)
                        throw new NotImplementedException("Save is not supported on this type of Geometry");


                    switch (_mType) {
                        case wkbGeometryType.wkbPolygon:
                        case wkbGeometryType.wkbPolygon25D:
                        case wkbGeometryType.wkbPolygonM:
                        case wkbGeometryType.wkbPolygonZM:
                            Geometry poly = feature.GetGeometryRef();
                            while (poly.GetGeometryCount()> 0) {
                                poly.RemoveGeometry(0);
                            }
                            foreach (Dataline lineFunc in polyFunc.Lines) {
                                Debug.Log($"FID : {lineFunc.GetFid<long>()}: GID : {lineFunc.GetGid<int>()}");
                                Geometry line = new(wkbGeometryType.wkbLinearRing);
                                line.AssignSpatialReference(AppState.Instance.MapProj);
                                line.FromCurve(lineFunc.Curve, AxisOrder.ENU);
                                line.CloseRings();
                                line.TransformTo(GetCrs());
                                poly.AddGeometryDirectly(line);
                                line.Dispose();
                            }

                            poly.Dispose();
                            break;
                        case wkbGeometryType.wkbMultiPolygon:
                        case wkbGeometryType.wkbMultiPolygon25D:
                        case wkbGeometryType.wkbMultiPolygonM:
                        case wkbGeometryType.wkbMultiPolygonZM:
                            Geometry parentGeom = feature.GetGeometryRef();
                            Geometry mPoly = parentGeom.GetGeometryRef(polyFunc.GetGid<int>());
                            mPoly.AssignSpatialReference(AppState.Instance.MapProj);
                            parentGeom.AddGeometryDirectly(mPoly);
                            for (int i = 0; i < mPoly.GetGeometryCount(); i++) {
                                mPoly.RemoveGeometry(i);
                            }
                            foreach (Dataline lineFunc in polyFunc.Lines) {
                                Geometry line = new(wkbGeometryType.wkbLinearRing);
                                line.AssignSpatialReference(AppState.Instance.MapProj);
                                line.FromCurve(lineFunc.Curve, AxisOrder.ENU);
                                line.CloseRings();
                                line.TransformTo(GetCrs());
                                mPoly.AddGeometryDirectly(line);
                                line.Dispose();
                            }

                            mPoly.Dispose();
                            parentGeom.Dispose();
                            break;
                        default:
                            throw new Exception("Layer Type Fault");
                    }

                    if (n) {
                        features.CreateFeature(feature);
                    } else {
                        features.SetFeature(feature);
                    }

                    features.Dispose();
                    if (watch.ElapsedMilliseconds < 100) continue;
                } catch (Exception e) {
                    Debug.LogError(e);
                }

                yield return null;
                watch.Restart();

            }

            features.SyncToDisk();
        }
    }
}
