using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;
using System.IO;
using Project;
using pdal;
using Newtonsoft.Json;

namespace Virgis
{


    public class PointCloudLayer : VirgisLayer<GeographyCollection, BakedPointCloud>
    {
        // The prefab for the data points to be instantiated
        public Material material;
        public GameObject handle;
        public GameObject pointCloud;
        public List<GameObject> meshes;
        public Material HandleMaterial;

        private GameObject model;
        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;

        private void Start() {
            featureType = FeatureType.POINTCLOUD;
        }


        protected override async Task _init() {
            Debug.Log("PC Start");
            GeographyCollection layer = _layer as GeographyCollection;
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

            pipe.Add(new {
                type = "filters.projpipeline",
                coord_op = "+proj=axisswap +order=1,-3,2"
            });

            if (layer.Properties.ColorInterp != null) {
                Dictionary<string, object> ci = new Dictionary<string, object>(layer.Properties.ColorInterp);
                ci.Add("type", "filters.colorinterp");
                pipe.Add(ci);
            }


            string json = JsonConvert.SerializeObject(new {
                pipeline = pipe.ToArray()
            });

            Pipeline pipeline = new Pipeline(json);
            if (pipeline.Valid == false)
                throw new System.NotSupportedException("Layer : " + layer.Id + "  - PDAL Pipeline is not valid - check Layer configuration");
            long pointCount = pipeline.Execute();
            PointViewIterator views = pipeline.Views;
            if (views != null) {
                pdal.PointView view = views != null ? views.Next : null;
                if (view != null) {
                    features = view.GetBakedPointCloud(pointCount);
                    view.Dispose();
                }
                views.Dispose();
            }
            pipeline.Dispose();

            symbology = layer.Properties.Units;

            Color col = symbology.ContainsKey("point") ? (Color)symbology["point"].Color : Color.white;
            Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            mainMat = Instantiate(HandleMaterial);
            mainMat.SetColor("_BaseColor", col);
            selectedMat = Instantiate(HandleMaterial);
            selectedMat.SetColor("_BaseColor", sel);
            Debug.Log("PC Finish");
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }

        protected override void _draw()
        {
            GeographyCollection layer = GetMetadata();
            transform.position = layer.Position != null ?  layer.Position.ToVector3() : Vector3.zero ;
            if (layer.Transform != null) transform.Translate(AppState.instance.map.transform.TransformVector((Vector3)layer.Transform.Position ));
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;

            model = Instantiate(pointCloud, transform, false);

            VisualEffect vfx = model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", features.positionMap);
            vfx.SetTexture("_Colors", features.colorMap);
            vfx.SetInt("_pointCount", features.pointCount);
            vfx.SetVector3("_size", symbology["point"].Transform.Scale);
            vfx.Play();

            if (layer.Transform != null) {
                transform.rotation = layer.Transform.Rotate;
                transform.localScale = layer.Transform.Scale;
                vfx.SetVector3("_scale", layer.Transform.Scale);
            }
            GameObject centreHandle = Instantiate(handle, transform.position, Quaternion.identity);
            centreHandle.transform.localScale = AppState.instance.map.transform.TransformVector((Vector3)symbology["handle"].Transform.Scale);
            centreHandle.GetComponent<Datapoint>().SetMaterial(mainMat, selectedMat);
            centreHandle.transform.parent = transform;
        }

        public override void _set_visible() {
            base._set_visible();
            VisualEffect vfx = model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", features.positionMap);
            vfx.SetTexture("_Colors", features.colorMap);
            vfx.SetInt("_pointCount", features.pointCount);
            vfx.SetVector3("_size", symbology["point"].Transform.Scale);
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
                VisualEffect vfx = model.GetComponent<VisualEffect>();
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
