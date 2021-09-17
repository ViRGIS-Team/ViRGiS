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
        public string fileName;
        public List<Feature> features;

        private readonly List<Layer> m_layers = new List<Layer>();
        private DataSource m_datasource;
        private int m_update;

        public List<Layer> GetLayers()
        {
            return m_layers;
        }

        public bool isWriteable {
            get {
                return m_update != 0;
            }
        }


        public async Task  Load(string source, int update, SourceType type) {
            if (type == SourceType.File) {
                fileName = source;
            } else if (type.ToString().Contains("vsi")) {
                fileName = $"\\{type}\\{source}";
            } else {
                fileName = $"{type}:{source}";
            }
            m_update = update;
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
                        m_datasource = Ogr.Open(fileName, m_update);
                        if (m_datasource == null)
                            throw (new FileNotFoundException());
                        for (int i = 0; i < m_datasource.GetLayerCount(); i++)
                            m_layers.Add(m_datasource.GetLayerByIndex(i));
                        if (m_layers.Count == 0)
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
            m_datasource?.Dispose();
        }
    }
}