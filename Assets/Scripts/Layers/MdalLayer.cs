using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Project;
using g3;
using Mdal;

namespace Virgis
{

    public class MdalLayer : VirgisLayer<GeographyCollection, List<DMesh3>>
    {
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
            symbology = layer.Properties.Units;
            Datasource ds = new Datasource(layer.Source);
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
        protected override void _draw()
        {
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

        public override void Translate(MoveArgs args) {
            if (args.translate != Vector3.zero)
                transform.position += args.translate;
                //transform.Translate(args.translate, Space.World);
            changed = true;
        }

        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
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
            _layer.Transform.Position = AppState.instance.map.transform.InverseTransformVector(transform.position);
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}

