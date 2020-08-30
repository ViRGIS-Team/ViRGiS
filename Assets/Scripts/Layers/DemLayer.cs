using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Project;
using pdal;
using Newtonsoft.Json;
using Mdal;
using g3;

namespace Virgis
{


    public class DemLayer : VirgisLayer<GeographyCollection, List<DMesh3>> {
        // The prefab for the data points to be instantiated
        public GameObject Mesh;
        public Material MeshMaterial;

        private List<Transform> meshes;
        private Dictionary<string, Unit> symbology;

        private void Start() {
            featureType = FeatureType.MESH;
        }


        protected override async Task _init() {
            GeographyCollection layer = _layer as GeographyCollection;
            string ex = Path.GetExtension(layer.Source).ToLower();
            string sourcetype = null;
            Datasource ds = null;

            if (".2dm .nc .dat .adf .out .grb .hdf .slf .sww .xdmf .xmdf .tin".Contains(ex))
                sourcetype = "mdal";
            else if (".bpf .json .e57 .mat .txt .las .nitf .npy .csd .pcd .ply .pts .qi .rxp .rdbx .sbet .slpk .bin .xyz ".Contains(ex) || new Regex(@"\.m\d\d").IsMatch(ex ))
                sourcetype = "pdal";
            else
                sourcetype = "gdal";

            if (sourcetype != "mdal") {
                List<object> pipe = new List<object>();
                if (sourcetype == "gdal")
                    pipe.Add(new {
                        type = "readers.gdal",
                        filename = layer.Source
                    });
                else if (ex == ".xyz")
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

                if (layer.Properties.ColorInterp != null) {
                    Dictionary<string, object> ci = new Dictionary<string, object>(layer.Properties.ColorInterp);
                    ci.Add("type", "filters.colorinterp");
                    ci.Add("dimension", "band-1");
                    pipe.Add(ci);
                }

                pipe.Add(new {
                    type = "filters.delaunay"
                });

                pipe.Add(new {
                    type = "writers.ply",
                    filename = $"test_data/{Path.GetFileNameWithoutExtension(layer.Source)}.tmp",
                    faces = true
                });

                string json = JsonConvert.SerializeObject(new {
                    pipeline = pipe.ToArray()
                });

                Pipeline pipeline = new Pipeline(json);
                if (pipeline.Valid == false)
                    throw new System.NotSupportedException("Layer : " + layer.Id + "  - PDAL Pipeline is not valid - check Layer configuration");
                long pointCount = pipeline.Execute();
                pipeline.Dispose();
                ds = new Datasource($"test_data/{Path.GetFileNameWithoutExtension(layer.Source)}.tmp");
            } else {
                ds = new Datasource(layer.Source);
            }
            features = new List<DMesh3>();
            for (int i = 0; i < ds.meshes.Length; i++) {
                DMesh3 mesh = ds.GetMesh(i);
                if (layer.ContainsKey("Crs") && layer.Crs != null) {
                    mesh.RemoveMetadata("CRS");
                    mesh.AttachMetadata("CRS", layer.Crs);
                };
                features.Add(mesh);
            }
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }

        protected override void _draw() {
            GeographyCollection layer = GetMetadata();
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                dMesh.CalculateUVs();
                meshes.Add(Instantiate(Mesh, transform).GetComponent<DataMesh>().Draw(dMesh, MeshMaterial));
            }
            transform.position = AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position);
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;

        }

        public override void _set_visible() {
            base._set_visible();
            
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
