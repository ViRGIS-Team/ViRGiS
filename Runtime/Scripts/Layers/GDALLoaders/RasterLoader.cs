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

using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;
using Project;
using Pdal;
using OSGeo.GDAL;
using Newtonsoft.Json;
using Stopwatch = System.Diagnostics.Stopwatch;
using VirgisGeometry;

namespace Virgis
{
    public class RasterLoader : VirgisLoader<BakedPointCloud>
    {
        private GameObject m_model;
        private double m_PixelSize;
        private const float m_PixelScaleFactor = 9;
        private RasterLayer parent;
        private Dictionary<string, Unit> m_Symbology;

        public override async Task _init() {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = _layer as RecordSet;
            m_Symbology = layer.Units;
            parent = m_parent as RasterLayer;
            await Load(layer);
            Debug.Log($"Raster Layer Load took : {stopWatch.Elapsed.TotalSeconds}");
        }

        protected async Task Load(RecordSet layer) {
            (long, Pipeline) result = await LoadAsync(layer);
            Pipeline pipeline = result.Item2;
            PointViewIterator views = pipeline.Views;
            if (views != null) {
                PointView view = views?.Next;
                if (view != null) {
                    features = await BakedPointCloud.Initialize(view);
                    view.Dispose();
                }
                views.Dispose();
            }
            pipeline.Dispose();
        }


        protected Task<(long, Pipeline)> LoadAsync(RecordSet layer) {

            Task<(long, Pipeline)> t1 = new(() => {
                Dataset raster = Gdal.Open(layer.Source, Access.GA_ReadOnly);
                Band band1 = raster.GetRasterBand(1);
                double scalingFactor = 0;
                List<object> pipe = new() {
                    new {
                        type = "readers.gdal",
                        filename = layer.Source,
                        header = layer.Properties.headerString
                    }
                };

                // Get the size and pixel size of the raster
                // if the raster has more than 1,000,000 data points, using poisson sampling to down size
                long datapoints = raster.RasterXSize * raster.RasterYSize;
                double[] geoTransform = new double[6];
                raster.GetGeoTransform(geoTransform);
                if (geoTransform == null && geoTransform[1] == 0) {
                    throw new Exception();
                }
                m_PixelSize = geoTransform[1];
                if (datapoints > 1000000) {
                    try {
                        scalingFactor = Math.Sqrt(datapoints / 1000000d * m_PixelSize);
                    } catch {
                        scalingFactor = Math.Sqrt(datapoints / 1000000d);
                    };
                    pipe.Add(new {
                        type = "filters.sample",
                        radius = scalingFactor
                    });
                }

                band1.FlushCache();
                band1.Dispose();
                raster.FlushCache();
                raster.Dispose();

                if (layer.Properties.Filter != null) {
                    foreach (Dictionary<string, object> item in layer.Properties.Filter)
                        pipe.Add(item);
                }

                if (layer.Properties.Dem != null) {
                    pipe.Add(new {
                        type = "filters.hag_dem",
                        raster = layer.Properties.Dem
                    });
                    pipe.Add(new {
                        type = "filters.ferry",
                        dimensions = "HeightAboveGround=>Z"
                    });
                } else {
                    pipe.Add(new {
                        type = "filters.ferry",
                        dimensions = "=>Z"
                    });
                }

                if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                    AppState.instance.mapProj.ExportToProj4(out string crs);
                    pipe.Add(new {
                        type = "filters.reprojection",
                        in_srs = layer.Crs,
                        out_srs = crs
                    });
                }

                pipe.Add(new {
                    type = "filters.projpipeline",
                    coord_op = "+proj=axisswap +order=1,-3,2"
                });
                
                if (m_Symbology.TryGetValue("body", out Unit bodySymbology) && 
                    bodySymbology.ColorMode == ColorMode.SinglebandColor && 
                    bodySymbology.ColorInterp != null) 
                {
                    Dictionary<string, object> ci = new(bodySymbology.ColorInterp) {
                        {
                            "type",
             "filters.colorinterp"
                        }
                    };
                    pipe.Add(ci);
                }


                string json = JsonConvert.SerializeObject(new {
                    pipeline = pipe.ToArray()
                });

                Pipeline pipeline = new(json);
                if (pipeline.Valid == false) {
                    Debug.LogError("Pipeline : " + json);
                    throw new System.NotSupportedException("Layer : " + layer.Id + "  - PDAL Pipeline is not valid - check Layer configuration");
                }
                long pointCount = pipeline.Execute();
                return (pointCount, pipeline);
            });
            t1.Start();
            return t1;
        }

        public override Task _draw()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = GetMetadata() as RecordSet;
            transform.position = layer.Position != null ?
                (Vector3)layer.Position.ToVector3d() : Vector3.zero ;
            if (layer.Transform != null) transform
                    .Translate(AppState.instance.Map.transform
                    .TransformVector((Vector3)layer.Transform.Position ));

            m_model = Instantiate(parent.pointCloud, transform, false);

            PointCloud com = m_model.GetComponent<PointCloud>();
            com.Spawn(transform);


            VisualEffect vfx = m_model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", features.PositionMap);
            vfx.SetTexture("_Colors", features.ColorMap);
            vfx.SetInt("_pointCount", features.PointCount);
            vfx.SetVector3("_size", (float)m_PixelSize * m_PixelScaleFactor * Vector3.one);
            vfx.Play();

            if (layer.Transform != null) {
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
                vfx.SetVector3("_scale", layer.Transform.Scale);
            }
            Debug.Log($"Raster Layer Draw took {stopWatch.Elapsed.TotalSeconds}");
            return Task.CompletedTask;
        }

        public override void _set_visible() {
            VisualEffect vfx = m_model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", features.PositionMap);
            vfx.SetTexture("_Colors", features.ColorMap);
            vfx.SetInt("_pointCount", features.PointCount);
            vfx.SetVector3("_size", (float) m_PixelSize * m_PixelScaleFactor * Vector3.one );
            vfx.Play();
        }

        public override void _checkpoint() { }

        public override Task _save()
        {
            _layer.Position = ((Vector3d)transform.position).ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}
