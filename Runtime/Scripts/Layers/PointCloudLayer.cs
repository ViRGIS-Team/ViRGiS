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
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;
using System.IO;
using Project;
using Pdal;
using Newtonsoft.Json;

namespace Virgis
{
    public class PointCloudLayer : VirgisLayer<RecordSet, BakedPointCloud>
    {
        // The prefab for the data points to be instantiated
        public Material material;
        public GameObject handle;
        public GameObject pointCloud;
        public List<GameObject> meshes;
        public Material HandleMaterial;

        private GameObject m_model;
        private Dictionary<string, Unit> m_symbology;
        private Material m_mainMat;
        private Material m_selectedMat;

        new protected void Awake() {
            base.Awake();
            featureType = FeatureType.POINTCLOUD;
        }

        protected override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            await Load(layer);
            m_symbology = layer.Properties.Units;
            Color col = m_symbology.ContainsKey("point") ? (Color) m_symbology["point"].Color : Color.white;
            Color sel = m_symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            m_mainMat = Instantiate(HandleMaterial);
            m_mainMat.SetColor("_BaseColor", col);
            m_selectedMat = Instantiate(HandleMaterial);
            m_selectedMat.SetColor("_BaseColor", sel);
        }

        protected async Task Load(RecordSet layer) {
            (long, Pipeline) result = await LoadAsync(layer);
            Pipeline pipeline = result.Item2;
            PointViewIterator views = pipeline.Views;
            if (views != null) {
                PointView view = views != null ? views.Next : null;
                if (view != null) {
                    features = await BakedPointCloud.Initialize(view);
                    view.Dispose();
                }
                views.Dispose();
            }
            pipeline.Dispose();
        }

        protected Task<(long, Pipeline)> LoadAsync(RecordSet layer) {
            Task<(long, Pipeline)> t1 = new Task<(long, Pipeline)>(() => {
                List<object> pipe = new List<object>();

                string ex = Path.GetExtension(layer.Source).ToLower();
                if (ex == ".xyz")
                    pipe.Add(new {
                        type = "readers.text",
                        filename = layer.Source,
                    });
                else
                    pipe.Add(layer.Source);

                if (layer.Properties.Filter != null) {
                    foreach (Dictionary<string, object> item in layer.Properties.Filter)
                        pipe.Add(item);
                }

                if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                    string crs;
                    AppState.instance.mapProj.ExportToProj4(out crs);
                    pipe.Add(new {
                        type = "filters.reprojection",
                        in_srs = layer.Crs,
                        out_srs = crs
                    });
                }

                if (layer.Properties.ColorInterp != null) {
                    Dictionary<string, object> ci = new Dictionary<string, object>(layer.Properties.ColorInterp);
                    ci.Add("type", "filters.colorinterp");
                    pipe.Add(ci);
                }

                pipe.Add(new {
                    type = "filters.projpipeline",
                    coord_op = "+proj=axisswap +order=1,3,2"
                });

                string json = JsonConvert.SerializeObject(new {
                    pipeline = pipe.ToArray()
                });

                Stopwatch stopWatch = Stopwatch.StartNew();
                Pipeline pipeline = new Pipeline(json);
                if (pipeline.Valid == false)
                    throw new System.NotSupportedException("Layer : " + layer.Id + "  - PDAL Pipeline is not valid - check Layer configuration");
                long pointCount = pipeline.Execute();
                UnityEngine.Debug.Log($"PointCloud PDAL took {stopWatch.Elapsed.TotalSeconds}");
                stopWatch.Stop();
                return (pointCount, pipeline);
            });
            t1.Start();
            return t1;
        }

        protected VirgisFeature _addFeature(Vector3[] geometry)
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
            vfx.SetVector3("_size", symbology["point"].Transform.Scale);
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
            UnityEngine.Debug.Log($"PointCloud Draw took {stopWatch.Elapsed.TotalSeconds}");
            return Task.CompletedTask;
        }

        public override void _set_visible() {
            base._set_visible();
            VisualEffect vfx = m_model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", features.PositionMap);
            vfx.SetTexture("_Colors", features.ColorMap);
            vfx.SetInt("_pointCount", features.PointCount);
            vfx.SetVector3("_size", m_symbology["point"].Transform.Scale);
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
