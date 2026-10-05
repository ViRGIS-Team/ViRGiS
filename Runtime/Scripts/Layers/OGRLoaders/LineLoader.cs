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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirgisGeometry;

namespace Virgis
{

    /// <summary>
    /// The parent entity for an instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLoader : LineLoaderPrototype<Layer>
    {
        
        private wkbGeometryType _mType;
        
        public override Task _init() {
            RecordSet layer = Layer as RecordSet;
            DataUnit = new() { Representation = DataUnitRepresent.Line };
            MSymbology = layer?.Units.ToDictionary(x => x.Key, x => (UnitPrototype)x.Value);
            ReadSymbology();
            return Task.CompletedTask;
        }

        public SpatialReference GetCrs() {
            return MCrs as SpatialReference;
        }

        public override async Task _draw()
        {
            RecordSet layer = GetMetadata()as RecordSet;
            if (layer?.Properties.BBox != null) {
                features.SetSpatialFilterRect(layer.Properties.BBox[0], layer.Properties.BBox[1], layer.Properties.BBox[2], layer.Properties.BBox[3]);
            }
            using (OgrReader ogrReader = new OgrReader()) {
                await ogrReader.GetFeaturesAsync(features);
                foreach (Feature feature in ogrReader.Features) {
                    if (feature == null)
                        continue;
                    int geoCount = feature.GetDefnRef().GetGeomFieldCount();
                    for (int j = 0; j < geoCount; j++) {
                        Geometry line = feature.GetGeomFieldRef(j);
                        if (line == null)
                            continue;
                        _mType = line.GetGeometryType();
                        DCurve3 curve;

                        switch (_mType) {
                            case wkbGeometryType.wkbLineString:
                            case wkbGeometryType.wkbLineString25D:
                            case wkbGeometryType.wkbLineStringM: 
                            case wkbGeometryType.wkbLineStringZM:
                                if (line.GetSpatialReference() == null)
                                    line.AssignSpatialReference(GetCrs());
                                curve = line.ToCurve(AppState.Instance.MapProj);
                                await _drawFeatureAsync(curve, feature.GetFID(), 0);
                                break;
                            case wkbGeometryType.wkbMultiLineString:
                            case wkbGeometryType.wkbMultiLineString25D:
                            case wkbGeometryType.wkbMultiLineStringM:
                            case wkbGeometryType.wkbMultiLineStringZM:
                                int n = line.GetGeometryCount();
                                for (int k = 0; k < n; k++) {
                                    Geometry line2 = line.GetGeometryRef(k);
                                    if (line2.GetSpatialReference() == null)
                                        line2.AssignSpatialReference(GetCrs());
                                    curve = line2.ToCurve(AppState.Instance.MapProj);
                                    await _drawFeatureAsync(curve, feature.GetFID(), k);
                                }

                                break;
                            default:
                                throw new Exception("Layer Type Fault");
                        }
                        line.Dispose();
                    }
                }
            }
            if (layer?.Transform != null) {
                transform.position = AppState.Instance.Map.transform.TransformPoint(layer.Transform.Position);
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
            }
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


        protected override IEnumerator Hydrate()
        {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            Dataline[] lineFuncs = gameObject.GetComponentsInChildren<Dataline>();
            foreach (Dataline lineFunc in lineFuncs) {
                try {
                    if (!lineFunc.changed) continue;
                    using IEnumerator<long> fids = lineFunc.Curve.GetDataItr<long>().GetEnumerator();

                    Feature feature = features.GetFeature(lineFunc.GetFid<long>());
                    bool n = false;
                    if (feature == null) {
                        feature = new Feature(features.GetLayerDefn());
                        n = true;
                    }

                    if (feature.GetDefnRef().GetGeomFieldCount() > 1)
                        throw new NotImplementedException("Save is not supported on this type of Geometry");
                    Geometry geom;

                    switch (_mType) {
                        case wkbGeometryType.wkbLineString:
                        case wkbGeometryType.wkbLineString25D:
                        case wkbGeometryType.wkbLineStringM:
                        case wkbGeometryType.wkbLineStringZM:
                            geom = new(_mType);
                            geom.AssignSpatialReference(AppState.Instance.MapProj);
                            geom.FromCurve(lineFunc.Curve, AxisOrder.ENU);
                            geom.TransformTo(GetCrs());
                            feature.SetGeometryDirectly(geom);
                            break;
                        case wkbGeometryType.wkbMultiLineString:
                        case wkbGeometryType.wkbMultiLineString25D:
                        case wkbGeometryType.wkbMultiLineStringM:
                        case wkbGeometryType.wkbMultiLineStringZM:
                            Geometry parentGeom = feature.GetGeometryRef();
                            wkbGeometryType type;
                            if (parentGeom.GetGeometryCount() > 0) {
                                type = parentGeom.GetGeometryRef(0).GetGeometryType();
                            } else {
                                type = wkbGeometryType.wkbLineString;
                            }

                            parentGeom.RemoveGeometry(lineFunc.GetGid<int>());
                            geom = new(type);
                            geom.AssignSpatialReference(AppState.Instance.MapProj);
                            geom.FromCurve(lineFunc.Curve, AxisOrder.ENU);
                            geom.TransformTo(GetCrs());
                            parentGeom.AddGeometryDirectly(geom);
                            parentGeom.Dispose();
                            break;
                        default:
                            throw new NotImplementedException("Save is not supported on this type of Geometry");
                    }

                    feature.SetGeometryDirectly(geom);
                    if (n) {
                        features.CreateFeature(feature);
                    } else {
                        features.SetFeature(feature);
                    }

                    features.Dispose();
                    geom.Dispose();
                    if (watch.ElapsedMilliseconds < 100) continue;
                } catch (Exception e) {
                    Debug.LogException(e);
                }

                yield return null;
                watch.Restart();
            }
            features.SyncToDisk();
        }
    }
}
