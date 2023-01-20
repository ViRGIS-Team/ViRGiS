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

using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;
using Project;
using Pdal;
using OSGeo.GDAL;
using Newtonsoft.Json;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Virgis
{
    public class RasterLayer : VirgisLayer<RecordSet, BakedPointCloud>
    {
        // The prefab for the data points to be instantiated

        public GameObject handle;
        public GameObject pointCloud;
        public List<GameObject> meshes;
        public Material HandleMaterial;

        private GameObject m_model;
        private Dictionary<string, Unit> m_symbology;
        private Material m_mainMat;
        private Material m_selectedMat;
        private double m_PixelSize;
        private const float m_PixelScaleFactor = 9; 

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.RASTER;
        }

        protected override async Task _init() {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = _layer as RecordSet;
            await Load(layer);
            m_symbology = layer.Properties.Units;
            Color col = m_symbology.ContainsKey("point") ? (Color) m_symbology["point"].Color : Color.white;
            Color sel = m_symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            m_mainMat = Instantiate(HandleMaterial);
            m_mainMat.SetColor("_BaseColor", col);
            m_selectedMat = Instantiate(HandleMaterial);
            m_selectedMat.SetColor("_BaseColor", sel);
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

                if (layer.Properties.ColorMode == ColorMode.SinglebandColor && layer.Properties.ColorInterp != null) {
                    Dictionary<string, object> ci = new(layer.Properties.ColorInterp) {
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

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }

        protected override Task _draw()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = GetMetadata();
            transform.position = layer.Position != null ?  layer.Position.ToVector3() : Vector3.zero ;
            if (layer.Transform != null) transform.Translate(AppState.instance.map.transform.TransformVector((Vector3)layer.Transform.Position ));
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;

            m_model = Instantiate(pointCloud, transform, false);



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
            GameObject centreHandle = Instantiate(handle, transform.position, Quaternion.identity);
            centreHandle.transform.localScale = AppState.instance.map.transform.TransformVector((Vector3)symbology["handle"].Transform.Scale);
            centreHandle.GetComponent<Datapoint>().SetMaterial(m_mainMat, m_selectedMat);
            centreHandle.transform.parent = transform;
            Debug.Log($"Raster Layer Draw took {stopWatch.Elapsed.TotalSeconds}");
            return Task.CompletedTask;
        }

        public override void _set_visible() {
            base._set_visible();
            VisualEffect vfx = m_model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", features.PositionMap);
            vfx.SetTexture("_Colors", features.ColorMap);
            vfx.SetInt("_pointCount", features.PointCount);
            vfx.SetVector3("_size", (float) m_PixelSize * m_PixelScaleFactor * Vector3.one );
            vfx.Play();
        }


        public override void Translate(MoveArgs args)
        {

            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            changed = true;
        }


        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {
            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 A = transform.localPosition;
            Vector3 B = transform.parent.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue)
            {
                transform.localScale = transform.localScale * RS;
                transform.localPosition = FP;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform T = transform.GetChild(i);
                    if (T.GetComponent<Datapoint>() != null)
                    {
                        T.localScale /= RS;
                    }
                }
                VisualEffect vfx = m_model.GetComponent<VisualEffect>();
                vfx.SetVector3("_scale", transform.localScale);
            }
            changed = true;
        }

        protected override void _checkpoint() { }

        protected override Task _save()
        {
            _layer.Position = transform.position.ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}
