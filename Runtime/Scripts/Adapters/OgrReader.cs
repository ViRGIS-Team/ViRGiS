// copyright Runette Software Ltd, 2020. All rights reserved
using OSGeo.OGR;
using Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using SpatialReference = OSGeo.OSR.SpatialReference;

namespace Virgis {


    public class OgrReader: IDisposable
    {
        private List<Layer> _layers = new List<Layer>();
        public string fileName;
        private DataSource _datasource;
        private int _update;
        public List<Feature> features;

        public List<Layer> GetLayers()
        {
            return _layers;
        }


        public async Task  Load(string source, int update, SourceType type) {
            if (type == SourceType.File) {
                fileName = source;
            } else if (type.ToString().Contains("vsi")) {
                fileName = $"\\{type}\\{source}";
            } else {
                fileName = $"{type}:{source}";
            }
            _update = update;
            await Load();
        }

        private Task<int> Load() {
            try
            {
                TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
                Task<int> t1 = tcs1.Task;
                t1.ConfigureAwait(false);

                // Start a background task that will complete tcs1.Task
                Task.Factory.StartNew(() =>
                {
                    try {
                        _datasource = Ogr.Open(fileName, _update);
                        if (_datasource == null)
                            throw (new FileNotFoundException());
                        for (int i = 0; i < _datasource.GetLayerCount(); i++)
                            _layers.Add(_datasource.GetLayerByIndex(i));
                        if (_layers.Count == 0)
                            throw (new NotSupportedException());
                        tcs1.SetResult(1);
                    } catch (Exception e) {
                        tcs1.SetException(e);
                    }
                });
                return t1;
            }
            catch (Exception e) 
            {
                Debug.LogError("Failed to Load" + fileName + " : " + e.ToString());
                throw e;
            }
        }

        public Task<int> GetFeaturesAsync(Layer layer) {

            TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
            Task<int> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {
                try {
                    layer.ResetReading();
                    features = new List<Feature>();
                    Feature f = null;
                    do {
                        f = layer.GetNextFeature();
                        if (f != null)
                            features.Add(f);
                    } while (f != null);
                    tcs1.SetResult(1);
                } catch (Exception e) {
                    tcs1.SetException(e);
                }
            });
            return t1;
        }

        public static void Flatten(ref wkbGeometryType type) {
            if (type != wkbGeometryType.wkbUnknown && type != wkbGeometryType.wkbNone) {
                Geometry geom = new Geometry(type);
                geom.FlattenTo2D();
                type = geom.GetGeometryType();
                switch (type) {
                    case wkbGeometryType.wkbMultiLineString:
                        type = wkbGeometryType.wkbLineString;
                        break;
                    case wkbGeometryType.wkbMultiPoint:
                        type = wkbGeometryType.wkbPoint;
                        break;
                    case wkbGeometryType.wkbMultiPolygon:
                        type = wkbGeometryType.wkbPolygon;
                        break;
                }
            }
            return;
        }

        public static SpatialReference getSR(Layer layer, RecordSet metadata) {
            if (metadata.Crs == null || metadata.Crs == "") {
                SpatialReference crs = layer.GetSpatialRef();
                if (crs != null)
                    return crs;
                return AppState.instance.projectCrs;
            }
            return Convert.TextToSR(metadata.Crs);
        }

        public void Dispose() {
            _datasource?.Dispose();
        }
    }
}